// #nullable enable
//
// namespace ZenECS.Core.Systems
// {
//     /// <summary>
//     /// Variable-step, pre-simulation stage.
//     /// <para>
//     /// 실행 시점:
//     /// - BeginFrame 단계에서 호출되며, 주로 Unity / 디바이스 / 네트워크 입력을
//     ///   ECS 친화적인 형태(버퍼, 메시지, 컴포넌트)로 변환하는 데 사용됩니다.
//     /// </para>
//     /// <para>
//     /// 특징:
//     /// - dt는 가변 값(프레임 시간)입니다.
//     /// - 결정론 코어(Fixed* 단계에서 사용하는 상태)를 직접 변경하기보다는
//     ///   RawInput, ViewClickEvent, UI 이벤트 같은 비결정론적 데이터를 기록하는 용도로 사용하는 것을 권장합니다.
//     /// </para>
//     /// </summary>
//     public interface IFrameSetupSystem : ISystem { }
//
//     /// <summary>
//     /// Fixed-step deterministic pre-simulation stage.
//     /// <para>
//     /// 실행 시점:
//     /// - 고정 틱 파이프라인의 FixedInput / FixedDecision 그룹에서 실행됩니다.
//     ///   (예: 입력 → MoveInput2D, heading(FixedRotation2D), Command 생성 등)
//     /// </para>
//     /// <para>
//     /// 특징:
//     /// - dt는 고정 시간(fixedDeltaTime)입니다.
//     /// - 결정론 코어의 “준비 단계”로, 입력/AI/의사결정을 통해
//     ///   이번 틱에 사용할 데이터(heading, Forward, Commands 등)를 계산하는 데 적합합니다.
//     /// - 실행 중에는 <c>IWorld</c>의 <c>SimulationTick</c>이
//     ///   “현재 실행 중인 틱 번호”를 나타내도록 설계하는 것을 권장합니다.
//     /// </para>
//     /// </summary>
//     public interface IFixedSetupSystem : ISystem { }
//
//     /// <summary>
//     /// Fixed-step deterministic simulation stage.
//     /// <para>
//     /// 실행 시점:
//     /// - 고정 틱 파이프라인의 FixedSimulation / FixedPost 그룹에서 실행됩니다.
//     ///   (예: 이동, 물리, 발사체, 충돌, 데미지 적용, 사망 정리 등)
//     /// </para>
//     /// <para>
//     /// 특징:
//     /// - dt는 고정 시간(fixedDeltaTime)입니다.
//     /// - 결정론적 시뮬레이션(락스텝, 리플레이, 서버 검증 등)에 적합하며,
//     ///   모든 게임 로직 상태 변화는 가능하면 이 단계에서만 수행하는 것을 권장합니다.
//     /// - 실행 중 <c>IWorld.SimulationTick</c>은 “이번 틱 번호”를 의미하도록 두면,
//     ///   이벤트 태깅, 타이머(expireTick), 주기적 처리 등에 직관적으로 사용할 수 있습니다.
//     /// </para>
//     /// </summary>
//     public interface IFixedRunSystem : ISystem { }
//
//     /// <summary>
//     /// Executed during the Presentation phase.
//     /// <para>
//     /// 실행 시점:
//     /// - LateFrame / Presentation 그룹에서 호출되며,
//     ///   보간, 렌더링, UI 업데이트, 데이터→뷰 동기화에 사용됩니다.
//     /// </para>
//     /// <para>
//     /// 특징:
//     /// - dt는 해당 프레임의 delta time(가변)이고,
//     ///   <paramref name="alpha"/>는 [0,1] 범위의 보간 인자입니다.
//     /// - 여기서는 World의 결정론 상태를 읽기 전용으로 사용하고,
//     ///   ECS 코어 상태를 변경하지 않는 것을 강하게 권장합니다.
//     ///   (변경이 필요하다면 뷰 전용 컴포넌트나 Unity 오브젝트, UI 상태로 한정)
//     /// </para>
//     /// </summary>
//     public interface IPresentationSystem : ISystem
//     {
//         /// <summary>
//         /// Execute the presentation logic with interpolation.
//         /// </summary>
//         /// <param name="w">The ECS world.</param>
//         /// <param name="dt">Delta time of the originating frame in seconds.</param>
//         /// <param name="alpha">Interpolation factor in [0,1] (1=current, 0=previous).</param>
//         void Run(IWorld w, float dt, float alpha);
//
//         /// <summary>
//         /// Default shim to satisfy <see cref="ISystem.Run(ZenECS.Core.IWorld, float)"/>.
//         /// Calls <see cref="Run(IWorld, float, float)"/> with <c>alpha = 1f</c>.
//         /// </summary>
//         void ISystem.Run(IWorld w, float dt) => Run(w, dt, 1f);
//     }
// }
