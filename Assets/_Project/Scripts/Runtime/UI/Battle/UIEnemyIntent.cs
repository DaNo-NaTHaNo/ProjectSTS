using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectStS.Data;
using ProjectStS.Battle;

namespace ProjectStS.UI
{
    /// <summary>
    /// 적 유닛의 행동 의도를 아이콘과 텍스트로 표시하는 UI 컴포넌트.
    /// AIDecision의 ActionType에 따라 공격/방어/대기 등을 시각적으로 나타낸다.
    /// </summary>
    public class UIEnemyIntent : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Intent Display")]
        [SerializeField] private Image _intentIcon;
        [SerializeField] private Image _intentBackground;
        [SerializeField] private TextMeshProUGUI _intentText;

        [Header("Speech Bubble")]
        [SerializeField] private GameObject _speechBubbleRoot;
        [SerializeField] private TextMeshProUGUI _speechText;

        [Header("Intent Colors")]
        [SerializeField] private Color _attackColor = new Color(0.898f, 0.224f, 0.208f, 1f);
        [SerializeField] private Color _defendColor = new Color(0.118f, 0.533f, 0.898f, 1f);
        [SerializeField] private Color _statusColor = new Color(0.557f, 0.141f, 0.667f, 1f);
        [SerializeField] private Color _buffColor = new Color(1f, 0.757f, 0.027f, 1f);
        [SerializeField] private Color _passColor = new Color(0.620f, 0.620f, 0.620f, 1f);

        #endregion

        #region Private Fields

        private const string ATTACK_LABEL = "공격";
        private const string DEFEND_LABEL = "방어";
        private const string STATUS_LABEL = "상태이상";
        private const string BUFF_LABEL = "강화";
        private const string PASS_LABEL = "대기";
        private const string UNKNOWN_LABEL = "???";

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            HideIntent();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 적의 행동 의도를 표시한다.
        /// </summary>
        /// <param name="decision">AI가 결정한 행동</param>
        public void ShowIntent(AIDecision decision)
        {
            gameObject.SetActive(true);

            switch (decision.ActionType)
            {
                case AIActionType.PlayCard:
                    ShowPlayCardIntent(decision);
                    break;

                case AIActionType.EarnCard:
                    SetIntentDisplay(BUFF_LABEL, _buffColor);
                    break;

                case AIActionType.Pass:
                    SetIntentDisplay(PASS_LABEL, _passColor);
                    break;

                default:
                    SetIntentDisplay(UNKNOWN_LABEL, _passColor);
                    break;
            }

            UpdateSpeechBubble(decision.SpeechLine);
        }

        /// <summary>
        /// 행동 의도 표시를 숨긴다.
        /// </summary>
        public void HideIntent()
        {
            gameObject.SetActive(false);

            if (_speechBubbleRoot != null)
            {
                _speechBubbleRoot.SetActive(false);
            }
        }

        #endregion

        #region Private Methods

        private void ShowPlayCardIntent(AIDecision decision)
        {
            if (string.IsNullOrEmpty(decision.CardId))
            {
                SetIntentDisplay(ATTACK_LABEL, _attackColor);
                return;
            }

            if (!Core.ServiceLocator.TryGet<DataManager>(out var dataManager))
            {
                SetIntentDisplay(ATTACK_LABEL, _attackColor);
                return;
            }

            CardData card = dataManager.GetCard(decision.CardId);

            if (card == null)
            {
                SetIntentDisplay(ATTACK_LABEL, _attackColor);
                return;
            }

            switch (card.cardType)
            {
                case CardType.Attack:
                    SetIntentDisplay(ATTACK_LABEL, _attackColor);
                    break;

                case CardType.Defend:
                    SetIntentDisplay(DEFEND_LABEL, _defendColor);
                    break;

                case CardType.StatusEffect:
                    SetIntentDisplay(STATUS_LABEL, _statusColor);
                    break;

                case CardType.InHandEffect:
                    SetIntentDisplay(BUFF_LABEL, _buffColor);
                    break;

                default:
                    SetIntentDisplay(ATTACK_LABEL, _attackColor);
                    break;
            }
        }

        private void SetIntentDisplay(string label, Color color)
        {
            if (_intentText != null)
            {
                _intentText.text = label;
            }

            if (_intentBackground != null)
            {
                _intentBackground.color = color;
            }

            if (_intentIcon != null)
            {
                _intentIcon.color = color;
            }
        }

        private void UpdateSpeechBubble(string speechLine)
        {
            if (_speechBubbleRoot == null)
            {
                return;
            }

            bool hasSpeech = !string.IsNullOrEmpty(speechLine);
            _speechBubbleRoot.SetActive(hasSpeech);

            if (hasSpeech && _speechText != null)
            {
                _speechText.text = speechLine;
            }
        }

        #endregion
    }
}
