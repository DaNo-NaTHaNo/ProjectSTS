using UnityEngine;
using UnityEngine.UI;
using ProjectStS.Battle;
using ProjectStS.Core;

namespace ProjectStS.UI
{
    /// <summary>
    /// 전투 행동 종료 및 전투 포기 버튼을 관리하는 UI 컴포넌트.
    /// 턴 종료 버튼은 PlayerAction 페이즈에서만 활성화되며,
    /// 전투 포기 버튼은 항상 접근 가능하고 UIPopup 확인을 거친다.
    /// </summary>
    public class UIBattleActions : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Buttons")]
        [SerializeField] private Button _endTurnButton;
        [SerializeField] private Button _surrenderButton;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (_endTurnButton != null)
            {
                _endTurnButton.onClick.AddListener(OnEndTurnClicked);
            }

            if (_surrenderButton != null)
            {
                _surrenderButton.onClick.AddListener(OnSurrenderClicked);
            }
        }

        private void OnDisable()
        {
            if (_endTurnButton != null)
            {
                _endTurnButton.onClick.RemoveListener(OnEndTurnClicked);
            }

            if (_surrenderButton != null)
            {
                _surrenderButton.onClick.RemoveListener(OnSurrenderClicked);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 플레이어 행동 페이즈 여부에 따라 턴 종료 버튼을 활성/비활성한다.
        /// </summary>
        /// <param name="isActive">PlayerAction 페이즈 여부</param>
        public void SetPlayerActionPhase(bool isActive)
        {
            if (_endTurnButton != null)
            {
                _endTurnButton.interactable = isActive;
            }
        }

        #endregion

        #region Private Methods

        private void OnEndTurnClicked()
        {
            if (!ServiceLocator.TryGet<BattleManager>(out var battleManager))
            {
                return;
            }

            battleManager.EndTurn();
        }

        private void OnSurrenderClicked()
        {
            UIPopup.ShowConfirm(
                "전투를 포기하시겠습니까?\n패배 처리됩니다.",
                OnSurrenderConfirmed);
        }

        private void OnSurrenderConfirmed()
        {
            if (!ServiceLocator.TryGet<BattleManager>(out var battleManager))
            {
                return;
            }

            battleManager.Surrender();
        }

        #endregion
    }
}
