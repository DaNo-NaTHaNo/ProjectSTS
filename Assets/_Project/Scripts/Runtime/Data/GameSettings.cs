using UnityEngine;

namespace ProjectStS.Data
{
    /// <summary>
    /// 게임 전역 설정을 에디터에서 관리하는 ScriptableObject.
    /// 주인공 ID, 파티 제한, 씬 이름 등 밸런스/구성 상수를 보유한다.
    /// </summary>
    [CreateAssetMenu(fileName = "GameSettings", menuName = "ProjectStS/Data/GameSettings")]
    public class GameSettings : ScriptableObject
    {
        #region Serialized Fields

        [Header("주인공 설정")]
        [SerializeField] private string _protagonistUnitId;

        [Header("파티 설정")]
        [SerializeField] private int _maxPartySize = 3;
        [SerializeField] private int _minPartySize = 1;
        [SerializeField] private int _minDeckSize = 4;
        [SerializeField] private int _maxDeckSize = 6;
        [SerializeField] private int _maxSkillCount = 1;
        [SerializeField] private int _maxItemSlots = 2;

        [Header("씬 이름")]
        [SerializeField] private string _lobbySceneName = "LobbyScene";
        [SerializeField] private string _stageSceneName = "StageScene";
        [SerializeField] private string _battleSceneName = "BattleScene";

        #endregion

        #region Public Properties

        /// <summary>
        /// 주인공 유닛의 ID. 파티 편성 시 필수 포함 검증에 사용된다.
        /// </summary>
        public string ProtagonistUnitId => _protagonistUnitId;

        /// <summary>
        /// 파티 최대 인원 수.
        /// </summary>
        public int MaxPartySize => _maxPartySize;

        /// <summary>
        /// 파티 최소 인원 수.
        /// </summary>
        public int MinPartySize => _minPartySize;

        /// <summary>
        /// 유닛 당 최소 덱 크기.
        /// </summary>
        public int MinDeckSize => _minDeckSize;

        /// <summary>
        /// 유닛 당 최대 덱 크기.
        /// </summary>
        public int MaxDeckSize => _maxDeckSize;

        /// <summary>
        /// 유닛 당 최대 장비 스킬 수.
        /// </summary>
        public int MaxSkillCount => _maxSkillCount;

        /// <summary>
        /// 유닛 당 최대 아이템 슬롯 수.
        /// </summary>
        public int MaxItemSlots => _maxItemSlots;

        /// <summary>
        /// 로비 씬 이름.
        /// </summary>
        public string LobbySceneName => _lobbySceneName;

        /// <summary>
        /// 스테이지 씬 이름.
        /// </summary>
        public string StageSceneName => _stageSceneName;

        /// <summary>
        /// 전투 씬 이름.
        /// </summary>
        public string BattleSceneName => _battleSceneName;

        #endregion
    }
}
