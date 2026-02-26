using System.Collections.Generic;
using UnityEngine;
using ProjectStS.Data;
using ProjectStS.Core;

namespace ProjectStS.Battle
{
    /// <summary>
    /// 전투 전체의 공유 상태 컨테이너.
    /// 아군/적 유닛, 턴/페이즈, 에너지, 웨이브, 결과 등 전투 런타임 데이터를 보유한다.
    /// </summary>
    public class BattleState
    {
        #region Public Properties

        /// <summary>
        /// 아군 유닛 목록.
        /// </summary>
        public List<BattleUnit> Allies { get; }

        /// <summary>
        /// 현재 웨이브의 적 유닛 목록.
        /// </summary>
        public List<BattleUnit> Enemies { get; }

        /// <summary>
        /// 아군 + 적 전체 유닛 목록.
        /// </summary>
        public List<BattleUnit> AllUnits
        {
            get
            {
                var all = new List<BattleUnit>(Allies.Count + Enemies.Count);
                all.AddRange(Allies);
                all.AddRange(Enemies);
                return all;
            }
        }

        /// <summary>
        /// 생존한 아군 유닛 목록.
        /// </summary>
        public List<BattleUnit> AliveAllies
        {
            get
            {
                var alive = new List<BattleUnit>(Allies.Count);
                for (int i = 0; i < Allies.Count; i++)
                {
                    if (Allies[i].IsAlive)
                    {
                        alive.Add(Allies[i]);
                    }
                }
                return alive;
            }
        }

        /// <summary>
        /// 생존한 적 유닛 목록.
        /// </summary>
        public List<BattleUnit> AliveEnemies
        {
            get
            {
                var alive = new List<BattleUnit>(Enemies.Count);
                for (int i = 0; i < Enemies.Count; i++)
                {
                    if (Enemies[i].IsAlive)
                    {
                        alive.Add(Enemies[i]);
                    }
                }
                return alive;
            }
        }

        /// <summary>
        /// 현재 턴 수 (1부터 시작).
        /// </summary>
        public int CurrentTurn { get; set; }

        /// <summary>
        /// 현재 전투 페이즈.
        /// </summary>
        public BattlePhase CurrentPhase { get; set; }

        /// <summary>
        /// 파티 합산 기본 에너지 (턴 시작 시 이 값으로 회복).
        /// </summary>
        public int BaseEnergy { get; set; }

        /// <summary>
        /// 현재 에너지.
        /// </summary>
        public int CurrentEnergy { get; set; }

        /// <summary>
        /// 현재 웨이브 번호 (1부터 시작).
        /// </summary>
        public int CurrentWave { get; set; }

        /// <summary>
        /// 총 웨이브 수.
        /// </summary>
        public int TotalWaves { get; set; }

        /// <summary>
        /// 웨이브별 적 조합 데이터.
        /// </summary>
        public List<EnemyCombinationData> WaveData { get; }

        /// <summary>
        /// 타임라인 연동용 이벤트 ID.
        /// </summary>
        public string EventId { get; set; }

        /// <summary>
        /// 전투 결과.
        /// </summary>
        public BattleResult Result { get; set; }

        /// <summary>
        /// 전투 종료 사유.
        /// </summary>
        public BattleEndReason EndReason { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// BattleState를 생성한다.
        /// </summary>
        public BattleState()
        {
            Allies = new List<BattleUnit>(3);
            Enemies = new List<BattleUnit>(5);
            WaveData = new List<EnemyCombinationData>(4);
            CurrentTurn = 0;
            CurrentPhase = BattlePhase.None;
            CurrentWave = 0;
            TotalWaves = 0;
            Result = BattleResult.None;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 전투 상태를 초기화한다. 파티 편성, 웨이브 데이터, 이벤트 ID를 설정한다.
        /// </summary>
        /// <param name="party">파티 편성 데이터 목록</param>
        /// <param name="waves">웨이브별 적 조합 데이터 목록</param>
        /// <param name="eventId">전투 이벤트 ID</param>
        public void Initialize(List<OwnedUnitData> party, List<EnemyCombinationData> waves, string eventId)
        {
            if (!ServiceLocator.TryGet<DataManager>(out var dataManager))
            {
                Debug.LogError("[BattleState] DataManager를 찾을 수 없습니다.");
                return;
            }

            Allies.Clear();
            Enemies.Clear();
            WaveData.Clear();

            EventId = eventId;
            CurrentTurn = 0;
            CurrentPhase = BattlePhase.None;
            Result = BattleResult.None;

            int totalEnergy = 0;

            for (int i = 0; i < party.Count; i++)
            {
                OwnedUnitData owned = party[i];
                UnitData unitData = dataManager.GetUnit(owned.unitId);

                if (unitData == null)
                {
                    Debug.LogWarning($"[BattleState] 유닛 ID '{owned.unitId}'을(를) 찾을 수 없습니다.");
                    continue;
                }

                BattleUnit ally = BattleUnit.CreateAlly(unitData, owned, owned.partyPosition);
                Allies.Add(ally);
                totalEnergy += ally.MaxEnergy;
            }

            BaseEnergy = totalEnergy;
            CurrentEnergy = 0;

            if (waves != null)
            {
                WaveData.AddRange(waves);
            }

            TotalWaves = WaveData.Count;
            CurrentWave = 0;

            if (TotalWaves > 0)
            {
                SetupWave(0);
            }
        }

        /// <summary>
        /// 지정 웨이브의 적 유닛을 생성하여 Enemies 목록에 추가한다.
        /// </summary>
        /// <param name="waveIndex">웨이브 인덱스 (0부터 시작)</param>
        public void SetupWave(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= WaveData.Count)
            {
                Debug.LogError($"[BattleState] 유효하지 않은 웨이브 인덱스: {waveIndex}");
                return;
            }

            if (!ServiceLocator.TryGet<DataManager>(out var dataManager))
            {
                Debug.LogError("[BattleState] DataManager를 찾을 수 없습니다.");
                return;
            }

            EnemyCombinationData wave = WaveData[waveIndex];

            Enemies.Clear();
            CurrentWave = waveIndex + 1;

            SpawnEnemy(wave.enemyUnit1, 1, dataManager);
            SpawnEnemy(wave.enemyUnit2, 2, dataManager);
            SpawnEnemy(wave.enemyUnit3, 3, dataManager);
            SpawnEnemy(wave.enemyUnit4, 4, dataManager);
            SpawnEnemy(wave.enemyUnit5, 5, dataManager);
        }

        #endregion

        #region Private Methods

        private void SpawnEnemy(string unitId, int position, DataManager dataManager)
        {
            if (string.IsNullOrEmpty(unitId))
            {
                return;
            }

            UnitData unitData = dataManager.GetUnit(unitId);

            if (unitData == null)
            {
                Debug.LogWarning($"[BattleState] 적 유닛 ID '{unitId}'을(를) 찾을 수 없습니다.");
                return;
            }

            BattleUnit enemy = BattleUnit.CreateEnemy(unitData, position);
            Enemies.Add(enemy);
        }

        #endregion
    }
}
