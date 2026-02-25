namespace ProjectStS.Data
{
    /// <summary>
    /// 게임 전체 흐름의 현재 페이즈를 나타내는 열거형.
    /// GameFlowController에서 관리한다.
    /// </summary>
    public enum GameFlowPhase
    {
        /// <summary>초기 상태</summary>
        None,

        /// <summary>부팅 중</summary>
        Boot,

        /// <summary>로비 화면</summary>
        Lobby,

        /// <summary>스테이지 탐험 중</summary>
        Stage,

        /// <summary>전투 진행 중</summary>
        Battle,

        /// <summary>비주얼 노벨 재생 중</summary>
        VisualNovel,

        /// <summary>보상 정산 중</summary>
        Settlement
    }
}
