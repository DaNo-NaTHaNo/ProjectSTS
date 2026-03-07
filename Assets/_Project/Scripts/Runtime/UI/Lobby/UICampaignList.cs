using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectStS.Data;

namespace ProjectStS.UI
{
    /// <summary>
    /// 캠페인 리스트. 활성(진행 중) 캠페인과 완료된 캠페인을 분리하여 표시한다.
    /// 각 캠페인 항목을 클릭하면 상세 화면으로 전환한다.
    /// </summary>
    public class UICampaignList : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Active Campaign List")]
        [SerializeField] private Transform _activeListContainer;
        [SerializeField] private TextMeshProUGUI _activeHeaderText;

        [Header("Completed Campaign List")]
        [SerializeField] private Transform _completedListContainer;
        [SerializeField] private TextMeshProUGUI _completedHeaderText;

        [Header("Entry Prefab")]
        [SerializeField] private GameObject _campaignEntryPrefab;

        [Header("Empty State")]
        [SerializeField] private TextMeshProUGUI _emptyActiveText;
        [SerializeField] private TextMeshProUGUI _emptyCompletedText;

        #endregion

        #region Private Fields

        private readonly List<CampaignEntryView> _activeEntries = new List<CampaignEntryView>(8);
        private readonly List<CampaignEntryView> _completedEntries = new List<CampaignEntryView>(8);

        #endregion

        #region Events

        /// <summary>
        /// 캠페인 항목이 클릭되었을 때 발행한다.
        /// </summary>
        public event Action<CampaignData> OnCampaignSelected;

        #endregion

        #region Public Methods

        /// <summary>
        /// 캠페인 리스트를 초기화한다.
        /// </summary>
        /// <param name="active">활성 캠페인 목록</param>
        /// <param name="completed">완료된 캠페인 목록</param>
        public void Initialize(List<CampaignData> active, List<CampaignData> completed)
        {
            ClearAll();
            RefreshList(active, completed);
        }

        /// <summary>
        /// 활성 리스트에 캠페인을 추가한다.
        /// </summary>
        /// <param name="campaign">추가할 캠페인 데이터</param>
        public void AddCampaign(CampaignData campaign)
        {
            if (campaign == null)
            {
                return;
            }

            CampaignEntryView entry = CreateEntry(campaign, _activeListContainer);
            _activeEntries.Add(entry);
            UpdateHeaders();
        }

        /// <summary>
        /// 캠페인을 활성 리스트에서 완료 리스트로 이동한다.
        /// </summary>
        /// <param name="campaign">이동할 캠페인 데이터</param>
        public void MoveToCompleted(CampaignData campaign)
        {
            if (campaign == null)
            {
                return;
            }

            // 활성 리스트에서 제거
            for (int i = _activeEntries.Count - 1; i >= 0; i--)
            {
                if (_activeEntries[i].CampaignId == campaign.id)
                {
                    DestroyEntry(_activeEntries[i]);
                    _activeEntries.RemoveAt(i);
                    break;
                }
            }

            // 완료 리스트에 추가
            CampaignEntryView entry = CreateEntry(campaign, _completedListContainer);
            _completedEntries.Add(entry);
            UpdateHeaders();
        }

        /// <summary>
        /// 전체 리스트를 갱신한다.
        /// </summary>
        /// <param name="active">활성 캠페인 목록</param>
        /// <param name="completed">완료된 캠페인 목록</param>
        public void RefreshList(List<CampaignData> active, List<CampaignData> completed)
        {
            ClearAll();

            if (active != null)
            {
                for (int i = 0; i < active.Count; i++)
                {
                    CampaignEntryView entry = CreateEntry(active[i], _activeListContainer);
                    _activeEntries.Add(entry);
                }
            }

            if (completed != null)
            {
                for (int i = 0; i < completed.Count; i++)
                {
                    CampaignEntryView entry = CreateEntry(completed[i], _completedListContainer);
                    _completedEntries.Add(entry);
                }
            }

            UpdateHeaders();
        }

        #endregion

        #region Private Methods

        private CampaignEntryView CreateEntry(CampaignData campaign, Transform parent)
        {
            GameObject go;

            if (_campaignEntryPrefab != null)
            {
                go = Instantiate(_campaignEntryPrefab, parent);
            }
            else
            {
                go = new GameObject(campaign.name, typeof(RectTransform), typeof(Button));
                go.transform.SetParent(parent, false);

                var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                textGo.transform.SetParent(go.transform, false);
            }

            TextMeshProUGUI label = go.GetComponentInChildren<TextMeshProUGUI>();

            if (label != null)
            {
                label.text = campaign.name;
            }

            Button btn = go.GetComponent<Button>();

            if (btn == null)
            {
                btn = go.AddComponent<Button>();
            }

            CampaignData capturedCampaign = campaign;
            btn.onClick.AddListener(() => OnCampaignSelected?.Invoke(capturedCampaign));

            return new CampaignEntryView(campaign.id, go);
        }

        private void DestroyEntry(CampaignEntryView entry)
        {
            if (entry.Root != null)
            {
                Destroy(entry.Root);
            }
        }

        private void ClearAll()
        {
            for (int i = 0; i < _activeEntries.Count; i++)
            {
                DestroyEntry(_activeEntries[i]);
            }

            _activeEntries.Clear();

            for (int i = 0; i < _completedEntries.Count; i++)
            {
                DestroyEntry(_completedEntries[i]);
            }

            _completedEntries.Clear();
        }

        private void UpdateHeaders()
        {
            if (_activeHeaderText != null)
            {
                _activeHeaderText.text = $"진행 중 ({_activeEntries.Count})";
            }

            if (_completedHeaderText != null)
            {
                _completedHeaderText.text = $"완료 ({_completedEntries.Count})";
            }

            if (_emptyActiveText != null)
            {
                _emptyActiveText.gameObject.SetActive(_activeEntries.Count == 0);
            }

            if (_emptyCompletedText != null)
            {
                _emptyCompletedText.gameObject.SetActive(_completedEntries.Count == 0);
            }
        }

        #endregion

        #region Inner Types

        /// <summary>
        /// 캠페인 리스트 항목의 뷰 참조.
        /// </summary>
        private readonly struct CampaignEntryView
        {
            public readonly string CampaignId;
            public readonly GameObject Root;

            public CampaignEntryView(string campaignId, GameObject root)
            {
                CampaignId = campaignId;
                Root = root;
            }
        }

        #endregion
    }
}
