using System;
using System.Collections.Generic;
using ProjectStS.Data;

namespace ProjectStS.Battle
{
    /// <summary>
    /// 손패 관리 클래스.
    /// 리필(4장까지 드로우), 리테인(미사용 카드 잔류), 카드 사용/추가를 담당한다.
    /// </summary>
    public class HandManager
    {
        #region Constants

        /// <summary>
        /// 턴 시작 시 손패를 리필하는 목표 장수.
        /// </summary>
        public const int HAND_REFILL_SIZE = 4;

        #endregion

        #region Private Fields

        private readonly List<RuntimeCard> _hand;

        #endregion

        #region Public Properties

        /// <summary>
        /// 현재 손패의 카드 수.
        /// </summary>
        public int HandCount => _hand.Count;

        /// <summary>
        /// 현재 손패 (읽기 전용 접근).
        /// </summary>
        public IReadOnlyList<RuntimeCard> Hand => _hand;

        #endregion

        #region Events

        /// <summary>
        /// 카드가 손패에 추가되었을 때 발행.
        /// </summary>
        public event Action<RuntimeCard> OnCardAddedToHand;

        /// <summary>
        /// 카드가 손패에서 제거되었을 때 발행.
        /// </summary>
        public event Action<RuntimeCard> OnCardRemovedFromHand;

        #endregion

        #region Constructor

        /// <summary>
        /// HandManager를 생성한다.
        /// </summary>
        public HandManager()
        {
            _hand = new List<RuntimeCard>(HAND_REFILL_SIZE + 4);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 턴 시작 시 손패를 HAND_REFILL_SIZE까지 리필한다.
        /// 이미 4장 이상이면 드로우하지 않는다.
        /// </summary>
        /// <param name="deckManager">덱 매니저</param>
        public void RefillHand(DeckManager deckManager)
        {
            while (_hand.Count < HAND_REFILL_SIZE)
            {
                RuntimeCard card = deckManager.Draw();

                if (card == null)
                {
                    break;
                }

                _hand.Add(card);
                OnCardAddedToHand?.Invoke(card);
            }
        }

        /// <summary>
        /// 카드를 사용한다. 손패에서 제거하고, isDisposable이면 소멸, 아니면 디스카드한다.
        /// UntilPlayed 보정도 초기화한다.
        /// </summary>
        /// <param name="card">사용할 카드</param>
        /// <param name="deckManager">덱 매니저</param>
        public void PlayCard(RuntimeCard card, DeckManager deckManager)
        {
            if (!_hand.Contains(card))
            {
                return;
            }

            _hand.Remove(card);
            OnCardRemovedFromHand?.Invoke(card);

            card.ClearPlayedModifications();

            if (card.BaseData.isDisposable)
            {
                deckManager.Dispose(card);
            }
            else
            {
                deckManager.Discard(card);
            }
        }

        /// <summary>
        /// 손패에 카드를 추가한다 (AddCard 효과 등).
        /// </summary>
        /// <param name="card">추가할 카드</param>
        public void AddToHand(RuntimeCard card)
        {
            if (card == null)
            {
                return;
            }

            _hand.Add(card);
            OnCardAddedToHand?.Invoke(card);
        }

        /// <summary>
        /// 인덱스로 손패의 카드를 조회한다.
        /// </summary>
        /// <param name="index">카드 인덱스</param>
        /// <returns>카드. 범위 초과 시 null.</returns>
        public RuntimeCard GetCard(int index)
        {
            if (index < 0 || index >= _hand.Count)
            {
                return null;
            }

            return _hand[index];
        }

        /// <summary>
        /// 손패에서 특정 CardType의 카드 목록을 반환한다.
        /// </summary>
        /// <param name="type">카드 타입</param>
        /// <returns>매칭된 카드 목록</returns>
        public List<RuntimeCard> GetCardsByType(CardType type)
        {
            var result = new List<RuntimeCard>(_hand.Count);

            for (int i = 0; i < _hand.Count; i++)
            {
                if (_hand[i].BaseData.cardType == type)
                {
                    result.Add(_hand[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// 손패에서 특정 속성의 카드 목록을 반환한다.
        /// </summary>
        /// <param name="element">속성 타입</param>
        /// <returns>매칭된 카드 목록</returns>
        public List<RuntimeCard> GetCardsByElement(ElementType element)
        {
            var result = new List<RuntimeCard>(_hand.Count);

            for (int i = 0; i < _hand.Count; i++)
            {
                if (_hand[i].BaseData.element == element)
                {
                    result.Add(_hand[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// 손패의 모든 카드를 반환한다 (읽기용).
        /// </summary>
        /// <returns>손패 카드 목록 복사본</returns>
        public List<RuntimeCard> GetAllCards()
        {
            return new List<RuntimeCard>(_hand);
        }

        /// <summary>
        /// 손패를 비운다 (전투 종료 시).
        /// </summary>
        public void Clear()
        {
            _hand.Clear();
        }

        #endregion
    }
}
