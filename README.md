# Project-Chronicles

## 공격 및 애니메이션 추가

`AttackController`가 공격 데이터와 입력 버퍼를 관리하고, `CharacterAnimator`가 데이터의
`Animation` 경로로 Animator 상태를 재생합니다. Z1 → Z2 → Z3 → Z4가 연결되어 있으며,
추가 입력이 없으면 현재 공격만 마치고 Idle/Walk/Run으로 돌아갑니다.
씬의 Theodore와 Theodore 프리팹 모두 공격 컴포넌트를 포함합니다.

새 공격은 다음 설정만으로 추가할 수 있습니다. 단계별 트리거나 C# 분기는 필요 없습니다.

1. 공격 AnimationClip의 Loop Time을 끄고 Animator의 `GroundAttack` 안에 상태를 추가합니다.
2. 해당 상태의 Motion Time을 활성화하고 Float 파라미터 `AttackProgress`를 지정합니다.
   상태 간 자동 전환은 추가하지 않습니다. 기존 공격 상태를 복제하면 이 설정이 유지됩니다.
3. `Character/Combat/AttackData` 에셋을 만들고 `Animation`에 전체 상태 경로를 입력합니다.
   예: `Base Layer.GroundAttack.Theodore_Z5`.
4. Startup/Active/Recovery와 Combo Open/Close를 초 단위로 설정하고,
   이전 공격의 Next Z 또는 Next X에 새 에셋을 연결합니다. 마지막 공격의 Next는 비워 둡니다.
   새로운 첫 공격은 AttackController의 Z1 또는 X1에 지정합니다.

공격 클립은 `AttackProgress = 경과 시간 / (Startup + Active + Recovery)`로 샘플링하므로
클립 길이가 달라도 전체 공격 시간에 맞춰 재생됩니다. 이동 애니메이션 경로는
CharacterAnimator Inspector의 Locomotion Animator States에서 바꿀 수 있습니다.
애니메이션의 특정 프레임에 타격을 맞추려면 Startup/Active도 그 비율에 맞춰 설정합니다.

입력은 Combo Open 이상, Combo Close 미만인 구간의 첫 유효 입력 하나만 저장합니다.
키를 계속 누르는 것으로는 다음 입력이 발생하지 않으며, 연결된 다음 공격이 없는 입력은
버퍼를 차지하지 않습니다. 다음 공격은 현재 공격 전체 시간이 끝나면 시작합니다.
Cancel Open을 이용한 후딜 취소와 착지 대기 공격의 종료 처리는 아직 구현되지 않았습니다.

`Tools > Chronicles > Validate Combat Animations`에서 Z1~Z4 참조, 입력 구간,
실제 스프라이트 샘플링, 콤보 종료 후 이동 상태 복귀, 비활성화 초기화를 검증할 수 있습니다.
이 검증은 현재 Theodore의 Z1~Z4 기본 설정을 대상으로 합니다.

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
