# Project-Chronicles

## 플레이어 이동 구조

- `PlayerMovementInput`: 좌우 입력, 더블 탭 달리기 판정, Input System 활성화/해제와 자원 정리.
- `PlayerHorizontalMovement`: 걷기/달리기 속도, 바라보는 방향, `FixedUpdate`에서 수평 속도 적용. 수직 속도는 유지.
- `PlayerStateMachine`: 실제 수평 속도와 달리기 여부로 `IDLE` / `WALK` / `RUN` 상태 결정.
- `PlayerAnimator`: `LateUpdate`에서 상태를 `IsWalk` / `IsRun`에 반영하고 스프라이트 좌우 반전 처리.

방향키를 한 번 누르면 걷고, 같은 방향을 0.2초 이내 두 번 누른 뒤 유지하면 달립니다.
키를 놓거나 양쪽 방향키를 동시에 누르면 달리기가 취소됩니다.
기본 걷기/달리기 속도는 각각 6/12이며 이동 컴포넌트에서, 더블 탭 간격은 입력 컴포넌트에서 설정합니다.
`Prototype` 씬과 `Theodore` 프리팹에 네 컴포넌트를 연결했습니다.

애니메이터만 호출하던 `IState`, `IdleState`, `WalkState`, `RunState`는 제거했습니다.
`CurrentState`는 `PlayerState` 열거형이며 기존 `DASH` 값은 실제 동작에 맞춰 `RUN`으로 정리했습니다.
`Jump()` 트리거는 유지하며 점프 이동과 공중 상태 판정은 아직 구현하지 않았습니다.

Unity Test Runner의 PlayMode에서 `Project_Chronicles.Tests.PlayerMovementTests`를 실행할 수 있습니다.
실제 입력 이벤트와 프리팹, 컴포넌트 활성화/비활성화를 사용하여 이동, 달리기, 애니메이션,
수직 속도 보존, 입력 재활성화를 검증합니다. 프레임 메서드는 테스트에서 명시적으로 호출합니다.
