using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using ProjectStS.Data;
using ProjectStS.Stage;
using ProjectStS.Core;

namespace ProjectStS.UI
{
    /// <summary>
    /// 월드맵 렌더러.
    /// UIHexTile 오브젝트 풀링과 카메라(뷰포트) 줌/팬을 관리하여
    /// 2,437개 이상의 육각 노드를 효율적으로 표시한다.
    /// </summary>
    public class UIWorldMap : MonoBehaviour
    {
        #region Constants

        private const float SQRT3 = 1.7320508f;
        private const float SQRT3_HALF = 0.8660254f;

        #endregion

        #region Serialized Fields

        [Header("Hex Tile Pool")]
        [SerializeField] private UIHexTile _tilePrefab;
        [SerializeField] private RectTransform _mapContainer;
        [SerializeField] private int _poolInitialSize = 300;

        [Header("Hex Layout")]
        [SerializeField] private float _hexSize = 40f;
        [SerializeField] private float _hexSpacing = 2f;

        [Header("Camera Control")]
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private float _minZoom = 0.3f;
        [SerializeField] private float _maxZoom = 2.0f;
        [SerializeField] private float _zoomSpeed = 0.1f;
        [SerializeField] private float _panToDuration = 0.5f;

        [Header("Viewport")]
        [SerializeField] private float _viewportPadding = 100f;

        #endregion

        #region Private Fields

        private readonly Queue<UIHexTile> _tilePool = new Queue<UIHexTile>(300);
        private readonly Dictionary<(int, int, int), UIHexTile> _activeTiles =
            new Dictionary<(int, int, int), UIHexTile>(300);

        private Dictionary<(int, int, int), HexNode> _grid;
        private ZoneManager _zoneManager;
        private DataManager _dataManager;
        private float _currentZoom = 1f;
        private UIHexTile _currentNodeTile;
        private Tweener _panTween;

        #endregion

        #region Events

        /// <summary>
        /// 타일 클릭 시 발행. StageUIController에서 구독한다.
        /// </summary>
        public event Action<HexNode> OnTileClicked;

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            KillPanTween();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 월드맵을 초기화한다.
        /// </summary>
        /// <param name="grid">전체 노드 그리드</param>
        /// <param name="zoneManager">구역 매니저</param>
        /// <param name="dataManager">데이터 매니저</param>
        public void Initialize(
            Dictionary<(int, int, int), HexNode> grid,
            ZoneManager zoneManager,
            DataManager dataManager)
        {
            _grid = grid;
            _zoneManager = zoneManager;
            _dataManager = dataManager;

            InitializePool();
            SetupMapContainerSize();
            SetupScrollEvents();
            UpdateVisibleTiles();
        }

        /// <summary>
        /// 뷰포트 내 타일만 활성화하고 뷰포트 밖 타일은 풀에 반환한다.
        /// </summary>
        public void UpdateVisibleTiles()
        {
            if (_grid == null || _mapContainer == null)
            {
                return;
            }

            Rect viewportRect = GetViewportRect();

            ReturnOutOfViewTiles(viewportRect);
            SpawnInViewTiles(viewportRect);
        }

        /// <summary>
        /// 단일 노드를 공개 처리한다.
        /// </summary>
        /// <param name="node">공개할 노드</param>
        public void RevealNode(HexNode node)
        {
            if (node == null)
            {
                return;
            }

            if (_activeTiles.TryGetValue(node.Key, out UIHexTile tile))
            {
                tile.UpdateVisualState();
                tile.PlayRevealAnimation();
            }
            else
            {
                Vector2 pixelPos = HexToPixel(node.Q, node.R);
                Rect viewportRect = GetViewportRect();

                if (IsInViewport(pixelPos, viewportRect))
                {
                    UIHexTile newTile = SpawnTile(node, pixelPos);

                    if (newTile != null)
                    {
                        newTile.PlayRevealAnimation();
                    }
                }
            }
        }

        /// <summary>
        /// 구역 전체를 공개 처리한다.
        /// </summary>
        /// <param name="areaId">구역 ID</param>
        public void RevealZone(string areaId)
        {
            if (_grid == null)
            {
                return;
            }

            UpdateVisibleTiles();
        }

        /// <summary>
        /// 지정 노드로 부드럽게 카메라를 이동한다.
        /// </summary>
        /// <param name="node">포커스 대상 노드</param>
        public void FocusOnNode(HexNode node)
        {
            if (node == null || _scrollRect == null || _mapContainer == null)
            {
                return;
            }

            KillPanTween();

            Vector2 targetPixel = HexToPixel(node.Q, node.R);
            Vector2 containerSize = _mapContainer.rect.size;
            RectTransform viewportRT = _scrollRect.viewport != null
                ? _scrollRect.viewport
                : (RectTransform)_scrollRect.transform;
            Vector2 viewportSize = viewportRT.rect.size;

            float scaledContainerW = containerSize.x * _currentZoom;
            float scaledContainerH = containerSize.y * _currentZoom;

            float scrollableW = scaledContainerW - viewportSize.x;
            float scrollableH = scaledContainerH - viewportSize.y;

            Vector2 targetNormalized = _scrollRect.normalizedPosition;

            if (scrollableW > 0f)
            {
                float offsetX = (targetPixel.x * _currentZoom) + (scaledContainerW * 0.5f) - (viewportSize.x * 0.5f);
                targetNormalized.x = Mathf.Clamp01(offsetX / scrollableW);
            }

            if (scrollableH > 0f)
            {
                float offsetY = (targetPixel.y * _currentZoom) + (scaledContainerH * 0.5f) - (viewportSize.y * 0.5f);
                targetNormalized.y = Mathf.Clamp01(offsetY / scrollableH);
            }

            _panTween = DOTween.To(
                () => _scrollRect.normalizedPosition,
                x => _scrollRect.normalizedPosition = x,
                targetNormalized,
                _panToDuration
            ).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                UpdateVisibleTiles();
            });
        }

        /// <summary>
        /// 특정 노드의 비주얼 상태를 갱신한다.
        /// </summary>
        /// <param name="node">갱신할 노드</param>
        public void UpdateNodeState(HexNode node)
        {
            if (node == null)
            {
                return;
            }

            if (_activeTiles.TryGetValue(node.Key, out UIHexTile tile))
            {
                tile.UpdateVisualState();
            }
        }

        /// <summary>
        /// 현재 위치 하이라이트를 업데이트한다.
        /// </summary>
        /// <param name="node">새 현재 위치 노드</param>
        public void SetCurrentNode(HexNode node)
        {
            if (_currentNodeTile != null)
            {
                _currentNodeTile.SetAsCurrent(false);
                _currentNodeTile.UpdateVisualState();
            }

            _currentNodeTile = null;

            if (node != null && _activeTiles.TryGetValue(node.Key, out UIHexTile tile))
            {
                tile.SetAsCurrent(true);
                _currentNodeTile = tile;
            }
        }

        #endregion

        #region Private Methods — Pool

        private void InitializePool()
        {
            if (_tilePrefab == null || _mapContainer == null)
            {
                Debug.LogError("[UIWorldMap] tilePrefab 또는 mapContainer가 할당되지 않았습니다.");
                return;
            }

            for (int i = 0; i < _poolInitialSize; i++)
            {
                UIHexTile tile = Instantiate(_tilePrefab, _mapContainer);
                tile.gameObject.SetActive(false);
                _tilePool.Enqueue(tile);
            }
        }

        private UIHexTile GetTileFromPool()
        {
            if (_tilePool.Count > 0)
            {
                return _tilePool.Dequeue();
            }

            UIHexTile tile = Instantiate(_tilePrefab, _mapContainer);
            tile.gameObject.SetActive(false);
            return tile;
        }

        private void ReturnTileToPool(UIHexTile tile)
        {
            tile.Unbind();
            _tilePool.Enqueue(tile);
        }

        #endregion

        #region Private Methods — Hex Layout

        private Vector2 HexToPixel(int q, int r)
        {
            float size = _hexSize + _hexSpacing;
            float x = size * 1.5f * q;
            float y = size * (SQRT3_HALF * q + SQRT3 * r);
            return new Vector2(x, y);
        }

        private void SetupMapContainerSize()
        {
            if (_mapContainer == null)
            {
                return;
            }

            float maxLevel = HexGridGenerator.DEFAULT_MAX_LEVEL;
            float size = _hexSize + _hexSpacing;
            float mapRadius = size * SQRT3 * (maxLevel + 1);
            float diameter = mapRadius * 2f;

            _mapContainer.sizeDelta = new Vector2(diameter, diameter);
            _mapContainer.pivot = new Vector2(0.5f, 0.5f);
        }

        #endregion

        #region Private Methods — Viewport

        private void SetupScrollEvents()
        {
            if (_scrollRect != null)
            {
                _scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
            }
        }

        private void OnScrollValueChanged(Vector2 value)
        {
            UpdateVisibleTiles();
        }

        private Rect GetViewportRect()
        {
            if (_scrollRect == null)
            {
                return new Rect(-5000f, -5000f, 10000f, 10000f);
            }

            RectTransform viewportRT = _scrollRect.viewport != null
                ? _scrollRect.viewport
                : (RectTransform)_scrollRect.transform;

            Vector2 viewportSize = viewportRT.rect.size;
            Vector2 contentPos = _mapContainer.anchoredPosition;
            float padding = _viewportPadding;

            float halfW = (viewportSize.x * 0.5f / _currentZoom) + padding;
            float halfH = (viewportSize.y * 0.5f / _currentZoom) + padding;

            float centerX = -contentPos.x / _currentZoom;
            float centerY = -contentPos.y / _currentZoom;

            return new Rect(centerX - halfW, centerY - halfH, halfW * 2f, halfH * 2f);
        }

        private bool IsInViewport(Vector2 position, Rect viewportRect)
        {
            return viewportRect.Contains(position);
        }

        private void ReturnOutOfViewTiles(Rect viewportRect)
        {
            List<(int, int, int)> toRemove = null;

            foreach (var kvp in _activeTiles)
            {
                Vector2 pos = HexToPixel(kvp.Key.Item1, kvp.Key.Item2);

                if (!IsInViewport(pos, viewportRect))
                {
                    if (toRemove == null)
                    {
                        toRemove = new List<(int, int, int)>(32);
                    }

                    toRemove.Add(kvp.Key);
                }
            }

            if (toRemove != null)
            {
                for (int i = 0; i < toRemove.Count; i++)
                {
                    UIHexTile tile = _activeTiles[toRemove[i]];

                    if (tile == _currentNodeTile)
                    {
                        _currentNodeTile = null;
                    }

                    ReturnTileToPool(tile);
                    _activeTiles.Remove(toRemove[i]);
                }
            }
        }

        private void SpawnInViewTiles(Rect viewportRect)
        {
            foreach (var kvp in _grid)
            {
                HexNode node = kvp.Value;

                if (!node.IsRevealed)
                {
                    continue;
                }

                if (_activeTiles.ContainsKey(kvp.Key))
                {
                    continue;
                }

                Vector2 pixelPos = HexToPixel(node.Q, node.R);

                if (!IsInViewport(pixelPos, viewportRect))
                {
                    continue;
                }

                SpawnTile(node, pixelPos);
            }
        }

        private UIHexTile SpawnTile(HexNode node, Vector2 pixelPos)
        {
            UIHexTile tile = GetTileFromPool();

            if (tile == null)
            {
                return null;
            }

            RectTransform tileRT = (RectTransform)tile.transform;
            tileRT.anchoredPosition = pixelPos;

            AreaData area = null;

            if (_zoneManager != null)
            {
                area = _zoneManager.GetAreaForNode(node);
            }

            tile.BindNode(node, area);
            tile.OnTileClicked += HandleTileClicked;

            _activeTiles[node.Key] = tile;

            return tile;
        }

        private void HandleTileClicked(UIHexTile tile)
        {
            if (tile.BoundNode != null)
            {
                OnTileClicked?.Invoke(tile.BoundNode);
            }
        }

        #endregion

        #region Private Methods — Zoom

        /// <summary>
        /// Update()는 줌 입력에만 사용. 이벤트 기반 처리가 불가능한 마우스 스크롤 입력.
        /// </summary>
        private void Update()
        {
            float scrollDelta = Input.mouseScrollDelta.y;

            if (Mathf.Abs(scrollDelta) > 0.01f)
            {
                HandleZoom(scrollDelta);
            }
        }

        private void HandleZoom(float delta)
        {
            float newZoom = Mathf.Clamp(
                _currentZoom + delta * _zoomSpeed,
                _minZoom,
                _maxZoom
            );

            if (Mathf.Approximately(newZoom, _currentZoom))
            {
                return;
            }

            _currentZoom = newZoom;

            if (_mapContainer != null)
            {
                _mapContainer.localScale = new Vector3(_currentZoom, _currentZoom, 1f);
            }

            UpdateVisibleTiles();
        }

        private void KillPanTween()
        {
            if (_panTween != null && _panTween.IsActive())
            {
                _panTween.Kill();
                _panTween = null;
            }
        }

        #endregion
    }
}
