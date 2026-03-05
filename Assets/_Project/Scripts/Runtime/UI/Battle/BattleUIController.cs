using System.Collections.Generic;
using UnityEngine;
using ProjectStS.Data;
using ProjectStS.Battle;
using ProjectStS.Core;

namespace ProjectStS.UI
{
    /// <summary>
    /// 전투 씬 UI의 중앙 컨트롤러.
    /// BattleUIBridge의 13개 이벤트를 구독하여 하위 UI 컴포넌트에 분배하고,
    /// 타겟 선택 시스템과 카드 사용 플로우를 관리한다.
    /// </summary>
    public class BattleUIController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Battle Manager")]
        [SerializeField] private BattleManager _battleManagerRef;

        [Header("Unit Panels")]
        [SerializeField] private UIBattleUnitPanel[] _allyPanels = new UIBattleUnitPanel[3];
        [SerializeField] private UIBattleUnitPanel[] _enemyPanels = new UIBattleUnitPanel[5];

        [Header("Sub Components")]
        [SerializeField] private UIHandArea _handArea;
        [SerializeField] private UIEnergyDisplay _energyDisplay;
        [SerializeField] private UIDeckCounter _deckCounter;
        [SerializeField] private UISkillNotify _skillNotify;
        [SerializeField] private UIBattleActions _battleActions;
        [SerializeField] private UIBattleResult _battleResult;

        #endregion

        #region Private Fields

        private BattleManager _battleManager;
        private BattleUIBridge _uiBridge;
        private UICard _selectedCard;
        private bool _isTargetSelectionMode;

        private readonly Dictionary<BattleUnit, UIBattleUnitPanel> _unitPanelMap
            = new Dictionary<BattleUnit, UIBattleUnitPanel>(8);

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            BattleManager manager = _battleManagerRef;

            if (manager == null)
            {
                ServiceLocator.TryGet<BattleManager>(out manager);
            }

            if (manager != null)
            {
                BindBattleManager(manager);
            }
        }

        private void OnDisable()
        {
            UnbindBattleManager();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 전투 UI를 초기화한다. BattleManager 연결 후 호출된다.
        /// </summary>
        public void InitializeBattleUI()
        {
            if (_battleManager == null)
            {
                return;
            }

            BattleState state = _battleManager.State;

            _unitPanelMap.Clear();
            InitializeUnitPanels(state.Allies, _allyPanels);
            InitializeUnitPanels(state.Enemies, _enemyPanels);

            if (_energyDisplay != null)
            {
                _energyDisplay.SetImmediate(state.CurrentEnergy, state.BaseEnergy);
            }

            if (_battleActions != null)
            {
                _battleActions.SetPlayerActionPhase(false);
            }

            CancelTargetSelection();

            if (_handArea != null)
            {
                _handArea.OnCardSelected += HandleCardSelected;
                _handArea.OnCardDraggedToTarget += HandleCardDraggedToTarget;
            }
        }

        #endregion

        #region Private Methods — Binding

        private void BindBattleManager(BattleManager manager)
        {
            _battleManager = manager;
            _uiBridge = manager.UIBridge;

            if (_uiBridge == null)
            {
                Debug.LogWarning("[BattleUIController] UIBridge가 null입니다.");
                return;
            }

            _uiBridge.OnPhaseChanged += HandlePhaseChanged;
            _uiBridge.OnCardAddedToHand += HandleCardAddedToHand;
            _uiBridge.OnCardRemovedFromHand += HandleCardRemovedFromHand;
            _uiBridge.OnCardPlayed += HandleCardPlayed;
            _uiBridge.OnUnitDamaged += HandleUnitDamaged;
            _uiBridge.OnUnitHealed += HandleUnitHealed;
            _uiBridge.OnUnitBlockGained += HandleUnitBlockGained;
            _uiBridge.OnUnitDefeated += HandleUnitDefeated;
            _uiBridge.OnStatusEffectChanged += HandleStatusEffectChanged;
            _uiBridge.OnEnergyChanged += HandleEnergyChanged;
            _uiBridge.OnEnemyIntentShown += HandleEnemyIntentShown;
            _uiBridge.OnSkillActivated += HandleSkillActivated;
            _uiBridge.OnBattleResultShown += HandleBattleResultShown;

            _battleManager.OnBattleInitialized += HandleBattleInitialized;

            if (_battleManager.IsInitialized)
            {
                InitializeBattleUI();
            }
        }

        private void UnbindBattleManager()
        {
            if (_handArea != null)
            {
                _handArea.OnCardSelected -= HandleCardSelected;
                _handArea.OnCardDraggedToTarget -= HandleCardDraggedToTarget;
            }

            if (_uiBridge != null)
            {
                _uiBridge.OnPhaseChanged -= HandlePhaseChanged;
                _uiBridge.OnCardAddedToHand -= HandleCardAddedToHand;
                _uiBridge.OnCardRemovedFromHand -= HandleCardRemovedFromHand;
                _uiBridge.OnCardPlayed -= HandleCardPlayed;
                _uiBridge.OnUnitDamaged -= HandleUnitDamaged;
                _uiBridge.OnUnitHealed -= HandleUnitHealed;
                _uiBridge.OnUnitBlockGained -= HandleUnitBlockGained;
                _uiBridge.OnUnitDefeated -= HandleUnitDefeated;
                _uiBridge.OnStatusEffectChanged -= HandleStatusEffectChanged;
                _uiBridge.OnEnergyChanged -= HandleEnergyChanged;
                _uiBridge.OnEnemyIntentShown -= HandleEnemyIntentShown;
                _uiBridge.OnSkillActivated -= HandleSkillActivated;
                _uiBridge.OnBattleResultShown -= HandleBattleResultShown;
            }

            if (_battleManager != null)
            {
                _battleManager.OnBattleInitialized -= HandleBattleInitialized;
            }

            _battleManager = null;
            _uiBridge = null;
        }

        #endregion

        #region Private Methods — Initialization

        private void HandleBattleInitialized()
        {
            InitializeBattleUI();
        }

        private void InitializeUnitPanels(List<BattleUnit> units, UIBattleUnitPanel[] panels)
        {
            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] == null)
                {
                    continue;
                }

                if (i < units.Count)
                {
                    BattleUnit unit = units[i];
                    panels[i].SetUnit(unit);
                    panels[i].OnUnitClicked += HandleUnitPanelClicked;
                    _unitPanelMap[unit] = panels[i];
                }
                else
                {
                    panels[i].Clear();
                }
            }
        }

        #endregion

        #region Private Methods — Event Handlers

        private void HandlePhaseChanged(BattlePhase phase)
        {
            bool isPlayerAction = phase == BattlePhase.PlayerAction;

            if (_battleActions != null)
            {
                _battleActions.SetPlayerActionPhase(isPlayerAction);
            }

            if (!isPlayerAction)
            {
                CancelTargetSelection();
            }

            if (phase == BattlePhase.EnemyAction)
            {
                HideAllEnemyIntents();
            }

            if (phase == BattlePhase.TurnStart)
            {
                RefreshAllUnitPanels();
            }

            if (isPlayerAction)
            {
                RefreshDeckCounter();
            }

            if (_handArea != null && _battleManager != null)
            {
                _handArea.UpdateCardInteractability(_battleManager.State.CurrentEnergy);
            }
        }

        private void HandleCardAddedToHand(RuntimeCard card)
        {
            if (_handArea != null)
            {
                _handArea.AddCard(card);
            }

            RefreshDeckCounter();
        }

        private void HandleCardRemovedFromHand(RuntimeCard card)
        {
            if (_handArea != null)
            {
                _handArea.RemoveCard(card);
            }

            RefreshDeckCounter();
        }

        private void HandleCardPlayed(BattleUnit caster, RuntimeCard card)
        {
            if (_handArea != null)
            {
                _handArea.PlayCardAnimation(card);
            }

            RefreshDeckCounter();
        }

        private void HandleUnitDamaged(BattleUnit unit, int damage)
        {
            if (_unitPanelMap.TryGetValue(unit, out UIBattleUnitPanel panel))
            {
                panel.UpdateHP(unit.CurrentHP, unit.MaxHP);
                panel.ShowDamageText(damage);
            }
        }

        private void HandleUnitHealed(BattleUnit unit, int amount)
        {
            if (_unitPanelMap.TryGetValue(unit, out UIBattleUnitPanel panel))
            {
                panel.UpdateHP(unit.CurrentHP, unit.MaxHP);
                panel.ShowHealText(amount);
            }
        }

        private void HandleUnitBlockGained(BattleUnit unit, int block)
        {
            if (_unitPanelMap.TryGetValue(unit, out UIBattleUnitPanel panel))
            {
                panel.UpdateBlock(unit.Block);
            }
        }

        private void HandleUnitDefeated(BattleUnit unit)
        {
            if (_unitPanelMap.TryGetValue(unit, out UIBattleUnitPanel panel))
            {
                panel.ShowDefeated();
            }
        }

        private void HandleStatusEffectChanged(BattleUnit unit, ActiveStatusEffect effect)
        {
            if (_unitPanelMap.TryGetValue(unit, out UIBattleUnitPanel panel))
            {
                panel.UpdateStatusEffects(unit.StatusEffects);
            }
        }

        private void HandleEnergyChanged(int currentEnergy)
        {
            if (_energyDisplay != null)
            {
                _energyDisplay.UpdateEnergy(currentEnergy);
            }

            if (_handArea != null)
            {
                _handArea.UpdateCardInteractability(currentEnergy);
            }
        }

        private void HandleEnemyIntentShown(BattleUnit enemy, AIDecision decision)
        {
            if (_unitPanelMap.TryGetValue(enemy, out UIBattleUnitPanel panel))
            {
                panel.ShowEnemyIntent(decision);
            }
        }

        private void HandleSkillActivated(BattleUnit unit, SkillData skill)
        {
            if (_skillNotify != null)
            {
                _skillNotify.EnqueueNotify(unit, skill);
            }
        }

        private void HandleBattleResultShown(BattleResult result, BattleEndReason reason)
        {
            CancelTargetSelection();

            if (_battleActions != null)
            {
                _battleActions.SetPlayerActionPhase(false);
            }

            if (_battleResult != null)
            {
                _battleResult.ShowResult(result, reason);
            }
        }

        #endregion

        #region Private Methods — Target Selection

        private void HandleCardSelected(UICard uiCard)
        {
            if (_battleManager == null || !_battleManager.IsPlayerActionPhase)
            {
                return;
            }

            RuntimeCard runtimeCard = uiCard.CurrentRuntimeCard;

            if (runtimeCard == null)
            {
                return;
            }

            if (_battleManager.State.CurrentEnergy < runtimeCard.ModifiedCost)
            {
                return;
            }

            if (_selectedCard == uiCard)
            {
                CancelTargetSelection();
                return;
            }

            CancelTargetSelection();
            _selectedCard = uiCard;
            uiCard.SetState(UICard.CardState.Selected);

            TargetSelectionRule rule = runtimeCard.BaseData.targetSelectionRule;

            if (rule == TargetSelectionRule.Manual)
            {
                EnterTargetSelectionMode(runtimeCard.BaseData.targetType);
            }
            else
            {
                int handIndex = _handArea.GetCardIndex(uiCard);
                ExecuteAutoTargetCard(handIndex);
            }
        }

        private void HandleCardDraggedToTarget(UICard uiCard)
        {
            HandleCardSelected(uiCard);
        }

        private void EnterTargetSelectionMode(TargetType targetType)
        {
            _isTargetSelectionMode = true;

            switch (targetType)
            {
                case TargetType.Enemy:
                    SetPanelsTargetable(_enemyPanels, true);
                    break;

                case TargetType.Ally:
                    SetPanelsTargetable(_allyPanels, true);
                    break;

                case TargetType.Both:
                    SetPanelsTargetable(_allyPanels, true);
                    SetPanelsTargetable(_enemyPanels, true);
                    break;
            }
        }

        private void HandleUnitPanelClicked(BattleUnit targetUnit)
        {
            if (!_isTargetSelectionMode || _selectedCard == null || _battleManager == null)
            {
                return;
            }

            int handIndex = _handArea != null ? _handArea.GetCardIndex(_selectedCard) : -1;

            if (handIndex < 0)
            {
                CancelTargetSelection();
                return;
            }

            var targets = new List<BattleUnit>(1) { targetUnit };
            CancelTargetSelection();

            _battleManager.PlayCard(handIndex, targets);
        }

        private void ExecuteAutoTargetCard(int handIndex)
        {
            if (_battleManager == null || handIndex < 0)
            {
                CancelTargetSelection();
                return;
            }

            CancelTargetSelection();
            _battleManager.PlayCard(handIndex, new List<BattleUnit>(0));
        }

        private void CancelTargetSelection()
        {
            if (_selectedCard != null)
            {
                _selectedCard.SetState(UICard.CardState.Normal);
                _selectedCard = null;
            }

            _isTargetSelectionMode = false;
            SetPanelsTargetable(_allyPanels, false);
            SetPanelsTargetable(_enemyPanels, false);
        }

        private void SetPanelsTargetable(UIBattleUnitPanel[] panels, bool targetable)
        {
            if (panels == null)
            {
                return;
            }

            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] != null && panels[i].BoundUnit != null && panels[i].BoundUnit.IsAlive)
                {
                    panels[i].SetTargetable(targetable);
                }
            }
        }

        #endregion

        #region Private Methods — Utility

        private void RefreshDeckCounter()
        {
            if (_deckCounter == null || _battleManager == null)
            {
                return;
            }

            DeckManager deckManager = _battleManager.DeckManager;

            if (deckManager == null)
            {
                return;
            }

            _deckCounter.UpdateCounts(deckManager.DrawPileCount, deckManager.DiscardPileCount);
        }

        private void RefreshAllUnitPanels()
        {
            if (_battleManager == null)
            {
                return;
            }

            BattleState state = _battleManager.State;

            for (int i = 0; i < state.Allies.Count && i < _allyPanels.Length; i++)
            {
                BattleUnit unit = state.Allies[i];

                if (_unitPanelMap.TryGetValue(unit, out UIBattleUnitPanel panel))
                {
                    panel.UpdateHP(unit.CurrentHP, unit.MaxHP);
                    panel.UpdateBlock(unit.Block);
                    panel.UpdateStatusEffects(unit.StatusEffects);
                }
            }

            for (int i = 0; i < state.Enemies.Count && i < _enemyPanels.Length; i++)
            {
                BattleUnit unit = state.Enemies[i];

                if (_unitPanelMap.TryGetValue(unit, out UIBattleUnitPanel panel))
                {
                    panel.UpdateHP(unit.CurrentHP, unit.MaxHP);
                    panel.UpdateBlock(unit.Block);
                    panel.UpdateStatusEffects(unit.StatusEffects);
                }
            }
        }

        private void HideAllEnemyIntents()
        {
            if (_enemyPanels == null)
            {
                return;
            }

            for (int i = 0; i < _enemyPanels.Length; i++)
            {
                if (_enemyPanels[i] != null)
                {
                    _enemyPanels[i].HideEnemyIntent();
                }
            }
        }

        #endregion
    }
}
