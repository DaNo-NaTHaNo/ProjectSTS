using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using ProjectStS.Data;
using ProjectStS.Stage;

namespace ProjectStS.UI
{
    /// <summary>
    /// 육각 타일 표시 위젯.
    /// 월드맵의 단일 HexNode를 시각적으로 표현하며, 오브젝트 풀링 대상이다.
    /// 노드 상태(미공개/공개/방문/현재위치/이벤트완료)에 따라 비주얼을 갱신한다.
    /// </summary>
    public class UIHexTile : MonoBehaviour, IPointerClickHandler
    {
        #region Constants

        private const float ALPHA_HIDDEN = 0f;
        private const float ALPHA_REVEALED = 0.5f;
        private const float ALPHA_VISITED = 0.3f;
        private const float ALPHA_CURRENT = 1f;
        private const float ALPHA_COMPLETED = 0.2f;

        private static readonly Color32 COLOR_BATTLE_NORMAL = new Color32(0xF4, 0x43, 0x36, 0xFF);
        private static readonly Color32 COLOR_BATTLE_ELITE = new Color32(0xFF, 0x98, 0x00, 0xFF);
        private static readonly Color32 COLOR_BATTLE_BOSS = new Color32(0x9C, 0x27, 0xB0, 0xFF);
        private static readonly Color32 COLOR_BATTLE_EVENT = new Color32(0xFF, 0x57, 0x22, 0xFF);
        private static readonly Color32 COLOR_VISUAL_NOVEL = new Color32(0x21, 0x96, 0xF3, 0xFF);
        private static readonly Color32 COLOR_ENCOUNTER = new Color32(0x4C, 0xAF, 0x50, 0xFF);
        private static readonly Color32 COLOR_DEFAULT_ZONE = new Color32(0x78, 0x78, 0x78, 0xFF);

        /// <summary>
        /// EventType별 아이콘 표시 문자와 색상 매핑.
        /// </summary>
        private static readonly Dictionary<EventType, (string label, Color32 color)> EVENT_TYPE_VISUALS =
            new Dictionary<EventType, (string, Color32)>(6)
            {
                { EventType.BattleNormal,  ("검",  COLOR_BATTLE_NORMAL) },
                { EventType.BattleElite,   ("★",  COLOR_BATTLE_ELITE) },
                { EventType.BattleBoss,    ("왕",  COLOR_BATTLE_BOSS) },
                { EventType.BattleEvent,   ("⚡", COLOR_BATTLE_EVENT) },
                { EventType.VisualNovel,   ("책",  COLOR_VISUAL_NOVEL) },
                { EventType.Encounter,     ("?",   COLOR_ENCOUNTER) },
            };

        #endregion

        #region Serialized Fields

        [Header("Visual Elements")]
        [SerializeField] private Image _background;
        [SerializeField] private Image _eventIcon;
        [SerializeField] private TextMeshProUGUI _eventIconLabel;
        [SerializeField] private Image _highlightBorder;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Settings")]
        [SerializeField] private float _revealAnimDuration = 0.3f;

        #endregion

        #region Private Fields

        private HexNode _boundNode;
        private Color _zoneColor = COLOR_DEFAULT_ZONE;

        #endregion

        #region Public Properties

        /// <summary>
        /// 현재 바인딩된 HexNode.
        /// </summary>
        public HexNode BoundNode => _boundNode;

        #endregion

        #region Events

        /// <summary>
        /// 타일 클릭 시 발행. UIWorldMap에서 구독한다.
        /// </summary>
        public event Action<UIHexTile> OnTileClicked;

        #endregion

        #region Public Methods

        /// <summary>
        /// HexNode 데이터를 바인딩하고 비주얼을 갱신한다.
        /// </summary>
        /// <param name="node">바인딩할 HexNode</param>
        /// <param name="area">노드의 구역 데이터 (null 허용)</param>
        public void BindNode(HexNode node, AreaData area)
        {
            _boundNode = node;
            _zoneColor = COLOR_DEFAULT_ZONE;

            if (area != null)
            {
                _zoneColor = GetZoneColor(area.areaCardinalPoint);
            }

            UpdateVisualState();
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 바인딩을 해제하고 풀 반환 상태로 초기화한다.
        /// </summary>
        public void Unbind()
        {
            _boundNode = null;
            OnTileClicked = null;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 노드 상태 변경에 따라 비주얼을 갱신한다.
        /// </summary>
        public void UpdateVisualState()
        {
            if (_boundNode == null)
            {
                return;
            }

            UpdateNodeVisual();
            UpdateEventIcon();
        }

        /// <summary>
        /// 현재 위치 하이라이트를 설정한다.
        /// </summary>
        /// <param name="isCurrent">현재 위치 여부</param>
        public void SetAsCurrent(bool isCurrent)
        {
            if (_highlightBorder != null)
            {
                _highlightBorder.enabled = isCurrent;
            }

            if (isCurrent && _boundNode != null)
            {
                UpdateNodeVisual();
            }
        }

        /// <summary>
        /// 노드 공개 시 페이드인 애니메이션을 재생한다.
        /// </summary>
        public void PlayRevealAnimation()
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _canvasGroup.alpha = 0f;
            _canvasGroup.DOFade(1f, _revealAnimDuration).SetEase(Ease.OutQuad);
        }

        #endregion

        #region Unity Lifecycle

        private void OnDisable()
        {
            if (_canvasGroup != null)
            {
                DOTween.Kill(_canvasGroup);
            }
        }

        #endregion

        #region IPointerClickHandler

        /// <summary>
        /// 타일 클릭 처리.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_boundNode == null)
            {
                return;
            }

            OnTileClicked?.Invoke(this);
        }

        #endregion

        #region Private Methods

        private void UpdateNodeVisual()
        {
            if (_background == null)
            {
                return;
            }

            float alpha;

            if (!_boundNode.IsRevealed)
            {
                alpha = ALPHA_HIDDEN;
            }
            else if (_boundNode.IsEventCompleted)
            {
                alpha = ALPHA_COMPLETED;
            }
            else if (_boundNode.IsVisited)
            {
                alpha = ALPHA_VISITED;
            }
            else
            {
                alpha = ALPHA_REVEALED;
            }

            Color bgColor = _zoneColor;
            bgColor.a = alpha;
            _background.color = bgColor;
        }

        private void UpdateEventIcon()
        {
            bool showIcon = _boundNode.IsRevealed
                && _boundNode.AssignedEvent != null
                && !_boundNode.IsBoundary
                && !_boundNode.IsEventCompleted;

            if (_eventIcon != null)
            {
                _eventIcon.enabled = showIcon;
            }

            if (_eventIconLabel != null)
            {
                _eventIconLabel.enabled = showIcon;
            }

            if (!showIcon)
            {
                return;
            }

            EventType eventType = _boundNode.AssignedEvent.eventType;

            if (EVENT_TYPE_VISUALS.TryGetValue(eventType, out var visual))
            {
                if (_eventIcon != null)
                {
                    _eventIcon.color = visual.color;
                }

                if (_eventIconLabel != null)
                {
                    _eventIconLabel.text = visual.label;
                }
            }
        }

        private Color GetZoneColor(string cardinalPoint)
        {
            if (string.IsNullOrEmpty(cardinalPoint))
            {
                return COLOR_DEFAULT_ZONE;
            }

            switch (cardinalPoint)
            {
                case "N":  return new Color32(0x4C, 0xAF, 0x50, 0xFF); // 녹
                case "NE": return new Color32(0x21, 0x96, 0xF3, 0xFF); // 청
                case "SE": return new Color32(0x9C, 0x27, 0xB0, 0xFF); // 보라
                case "S":  return new Color32(0xF4, 0x43, 0x36, 0xFF); // 적
                case "SW": return new Color32(0xFF, 0x98, 0x00, 0xFF); // 주황
                case "NW": return new Color32(0xFF, 0xC1, 0x07, 0xFF); // 금
                default:   return COLOR_DEFAULT_ZONE;
            }
        }

        #endregion
    }
}
