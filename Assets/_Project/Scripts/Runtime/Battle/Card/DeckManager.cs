using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectStS.Data;
using ProjectStS.Core;

namespace ProjectStS.Battle
{
    /// <summary>
    /// Queue 기반 전투 덱 관리 클래스.
    /// 드로우 파일, 디스카드 파일, 셔플, 덱 생성(파티원 카드 합산 + 덱 압축)을 담당한다.
    /// </summary>
    public class DeckManager
    {
        #region Private Fields

        private readonly Queue<RuntimeCard> _drawPile;
        private readonly List<RuntimeCard> _discardPile;
        private readonly System.Random _random;

        #endregion

        #region Public Properties

        /// <summary>
        /// 드로우 파일의 카드 수.
        /// </summary>
        public int DrawPileCount => _drawPile.Count;

        /// <summary>
        /// 디스카드 파일의 카드 수.
        /// </summary>
        public int DiscardPileCount => _discardPile.Count;

        #endregion

        #region Events

        /// <summary>
        /// 카드가 드로우되었을 때 발행.
        /// </summary>
        public event Action<RuntimeCard> OnCardDrawn;

        /// <summary>
        /// 카드가 디스카드되었을 때 발행.
        /// </summary>
        public event Action<RuntimeCard> OnCardDiscarded;

        /// <summary>
        /// 덱이 셔플되었을 때 발행.
        /// </summary>
        public event Action OnDeckShuffled;

        /// <summary>
        /// Disposable 카드가 소멸되었을 때 발행.
        /// </summary>
        public event Action<RuntimeCard> OnCardDisposed;

        #endregion

        #region Constructor

        /// <summary>
        /// DeckManager를 생성한다.
        /// </summary>
        public DeckManager()
        {
            _drawPile = new Queue<RuntimeCard>(18);
            _discardPile = new List<RuntimeCard>(18);
            _random = new System.Random();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 파티원의 덱을 합산하여 전투 덱을 구축한다.
        /// 파티 인원수에 따른 덱 압축 규칙을 적용하고 셔플한다.
        /// </summary>
        /// <param name="state">전투 상태</param>
        public void BuildBattleDeck(BattleState state)
        {
            if (!ServiceLocator.TryGet<DataManager>(out var dataManager))
            {
                Debug.LogError("[DeckManager] DataManager를 찾을 수 없습니다.");
                return;
            }

            _drawPile.Clear();
            _discardPile.Clear();

            int partySize = state.Allies.Count;
            int maxCardsPerUnit = GetMaxCardsPerUnit(partySize);

            var allCards = new List<RuntimeCard>(18);

            for (int i = 0; i < state.Allies.Count; i++)
            {
                BattleUnit ally = state.Allies[i];
                int cardCount = 0;

                for (int j = 0; j < ally.DeckCardIds.Count; j++)
                {
                    if (cardCount >= maxCardsPerUnit)
                    {
                        break;
                    }

                    string cardId = ally.DeckCardIds[j];
                    CardData cardData = dataManager.GetCard(cardId);

                    if (cardData == null)
                    {
                        Debug.LogWarning($"[DeckManager] 카드 ID '{cardId}'을(를) 찾을 수 없습니다.");
                        continue;
                    }

                    var runtimeCard = new RuntimeCard(cardData, ally.UnitId);
                    allCards.Add(runtimeCard);
                    cardCount++;
                }
            }

            ShuffleList(allCards);

            for (int i = 0; i < allCards.Count; i++)
            {
                _drawPile.Enqueue(allCards[i]);
            }

            Debug.Log($"[DeckManager] 전투 덱 구축 완료: {_drawPile.Count}장 (파티 {partySize}인, 유닛당 최대 {maxCardsPerUnit}장)");
        }

        /// <summary>
        /// 드로우 파일에서 카드 1장을 드로우한다.
        /// 드로우 파일이 비어있으면 디스카드 파일을 리셔플한다.
        /// </summary>
        /// <returns>드로우한 카드. 드로우 불가 시 null.</returns>
        public RuntimeCard Draw()
        {
            if (_drawPile.Count == 0)
            {
                Reshuffle();
            }

            if (_drawPile.Count == 0)
            {
                Debug.LogWarning("[DeckManager] 드로우할 카드가 없습니다.");
                return null;
            }

            RuntimeCard card = _drawPile.Dequeue();
            OnCardDrawn?.Invoke(card);
            return card;
        }

        /// <summary>
        /// 드로우 파일에서 지정 수만큼 카드를 드로우한다.
        /// </summary>
        /// <param name="count">드로우할 카드 수</param>
        /// <returns>드로우한 카드 목록</returns>
        public List<RuntimeCard> Draw(int count)
        {
            var drawn = new List<RuntimeCard>(count);

            for (int i = 0; i < count; i++)
            {
                RuntimeCard card = Draw();

                if (card == null)
                {
                    break;
                }

                drawn.Add(card);
            }

            return drawn;
        }

        /// <summary>
        /// 카드를 디스카드 파일에 추가한다.
        /// </summary>
        /// <param name="card">디스카드할 카드</param>
        public void Discard(RuntimeCard card)
        {
            if (card == null)
            {
                return;
            }

            _discardPile.Add(card);
            OnCardDiscarded?.Invoke(card);
        }

        /// <summary>
        /// Disposable 카드를 게임에서 완전히 제거한다 (묘지에도 가지 않음).
        /// </summary>
        /// <param name="card">소멸시킬 카드</param>
        public void Dispose(RuntimeCard card)
        {
            if (card == null)
            {
                return;
            }

            OnCardDisposed?.Invoke(card);
        }

        /// <summary>
        /// 디스카드 파일의 카드를 셔플하여 드로우 파일에 추가한다.
        /// </summary>
        public void Reshuffle()
        {
            if (_discardPile.Count == 0)
            {
                return;
            }

            ShuffleList(_discardPile);

            for (int i = 0; i < _discardPile.Count; i++)
            {
                _drawPile.Enqueue(_discardPile[i]);
            }

            _discardPile.Clear();

            OnDeckShuffled?.Invoke();
        }

        /// <summary>
        /// AddCard 효과로 새 카드를 드로우 파일에 추가한다.
        /// </summary>
        /// <param name="cardId">추가할 카드 ID</param>
        /// <param name="ownerUnitId">소유자 유닛 ID</param>
        public void AddCardToDeck(string cardId, string ownerUnitId)
        {
            if (!ServiceLocator.TryGet<DataManager>(out var dataManager))
            {
                Debug.LogError("[DeckManager] DataManager를 찾을 수 없습니다.");
                return;
            }

            CardData cardData = dataManager.GetCard(cardId);

            if (cardData == null)
            {
                Debug.LogWarning($"[DeckManager] 덱에 추가할 카드 ID '{cardId}'을(를) 찾을 수 없습니다.");
                return;
            }

            var runtimeCard = new RuntimeCard(cardData, ownerUnitId);
            _drawPile.Enqueue(runtimeCard);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 파티 인원수에 따른 유닛당 최대 카드 수를 반환한다.
        /// 1인=6장, 2인=5장, 3인=4장.
        /// </summary>
        private int GetMaxCardsPerUnit(int partySize)
        {
            switch (partySize)
            {
                case 1: return 6;
                case 2: return 5;
                case 3: return 4;
                default: return 4;
            }
        }

        /// <summary>
        /// Fisher-Yates 알고리즘으로 리스트를 셔플한다.
        /// </summary>
        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        #endregion
    }
}
