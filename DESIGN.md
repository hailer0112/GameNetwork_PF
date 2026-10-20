# 설계와 학습 설명

## 역할과 데이터 흐름

입력(CoinPlayer 소유자) → 이동/획득 ServerRpc → 서버 CharacterController 및 CoinSession 판정 → NetworkVariable → 모든 실행 화면.
CoinMenu는 접속/상태/시험 UI, CoinRules는 변하지 않는 코인 ID와 위치, CoinSession은 서버 게임 규칙/공유 상태, CoinPlayer는 입력과 플레이어 통신입니다.
CoinGameBuilder는 런타임과 분리된 Editor 씬/프리팹 생성 도구입니다.
CoinMenu의 UniTask 대기는 연결 콜백으로 pending 상태가 바뀔 때 종료되며, 오브젝트 파괴 시 취소됩니다. 10초 시간 제한은 pending을 종료하고 네트워크를 정리합니다.
UniTask 출처: https://github.com/Cysharp/UniTask/commit/a77a1b4a89824fb6df293099de3060daf2a432c1 (수업 고정 버전 미기재에 따른 구현 선택). 2.5.10의 Editor TreeView API 호환 오류를 실제 확인한 후 공식 호환 수정 커밋으로 고정했습니다.

Host는 서버와 자기 Client를 동시에 실행합니다. 별도 Client는 본인 플레이어의 입력만 전송합니다.
CharacterController와 최종 위치 계산은 서버에서만 수행하므로 Client가 임의 위치를 보내 코인 거리 검사를 우회하는 경로가 없습니다.
이동 입력은 50ms마다 보내며 서버는 길이를 1로 제한하고, NaN/Infinity를 거절합니다. 300ms 동안 새 입력이 없으면 정지합니다.
최종 위치는 서버 쓰기 전용 NetworkVariable<Vector3>, 원격 화면은 보간 표시합니다. 예측/지연 보상은 범위 밖입니다.

## 코인/점수

12개 코인은 고정 ID 0~11. 서버 CollectedMask의 각 비트가 획득 여부입니다. Score는 공동 점수입니다.
서버가 ID 범위, 기존 획득 여부, 서버 플레이어 위치로부터 1.8m 이내 거리를 검사합니다.
Unity 서버 메인 스레드의 한 RPC 처리에서 비트를 먼저 변경하고 점수를 증가시키므로 다음 요청은 이미 획득한 코인을 발견합니다.
ServerRpc의 기본 소유권 검사를 사용해 다른 플레이어를 통한 요청을 막습니다.
획득 결과 메시지는 해당 소유자에게만 ClientRpc로 전달합니다. 점수/비트는 모든 Client에 동기화됩니다.
새 Client/재접속자는 현재 NetworkVariable 값을 받습니다. Host가 살아 있는 세션 동안만 점수와 코인이 유지됩니다.

## UDP 규약

UTF-8, 최대 256바이트, 한 datagram에 `POS1|peerId|sequence|x|y`.
예: `POS1|A|12|3|5`. POS1은 규약 버전, ID는 영숫자/_/- 1~32자, sequence는 양수 Int64, X/Y는 Int32.
포트/주소는 시작 인자로 받고 구성된 상대 엔드포인트에서 온 데이터만 허용합니다. 자기 ID 수신도 거절합니다.
새 위치마다 순서 번호 증가. 피어별 Dictionary에 마지막 적용 메시지를 저장하고 새 번호가 기존 이하이면 무시합니다.
이는 최신 상태 적용 규칙이며 전송 보장/재전송/인증을 제공하지 않습니다.
입력/송신 루프와 비동기 수신 작업이 분리되어 상대가 아무것도 보내지 않아도 입력할 수 있습니다.
마지막 20,000개 송신 메시지를 시험용으로 보관하여 원래 번호와 좌표를 재전송합니다.
로그에는 송수신, 파싱/엔드포인트 거절, 오래된 번호 무시, 마지막 위치, 경과 시간을 남깁니다.

## 상태/측정 해석

게임 점수/코인/위치와 UDP 최신 위치는 실시간 상태입니다. 로그/설계/시험 결과는 제출 증거로 파일에 보관합니다.
시험 종료 시 수신 수가 송신 수보다 적으면 관찰 구간 내 미수신입니다. 아직 도착하지 않은 데이터와 확정 손실을 구분합니다.
부하 시험은 같은 POS1 규약과 증가 번호를 사용하고 각 메시지에 새 좌표를 넣습니다.
같은 100개 메시지를 10ms 간격/0ms 간격으로 비교하여 전송 간격의 효과를 측정합니다. 추가로 1000개 즉시 전송을 관찰합니다.
Windows 타이머/콘솔 출력 비용 때문에 지정 간격과 실측 시간은 일치하지 않을 수 있습니다.

## 참고/직접 작성 범위

참고: Unity-Program-Lecture/csharp_basic의 10.GameNetwork 제출 안내, DAY05/08의 UDP/순서 번호, DAY09의 접속/CharacterController, DAY10/11의 NetworkVariable/RPC/서버 판정, DAY12의 연결 수명주기, DAY14/15의 검증 원칙.
https://github.com/Unity-Program-Lecture/csharp_basic

이 제출물의 CoinSession, CoinRules, CoinPlayer, CoinMenu, CoinSmoke, CoinGameBuilder, UDP 규약/프로그램/시험 스크립트/문서는 Codex가 새로 작성했습니다.
강사님의 완성 씬/프리팹/폰트/런타임 코드는 복사하지 않았습니다. ProjectSettings의 시작점은 사용자의 기존 프로젝트입니다.
서버가 이동까지 결정하는 선택은 수업의 소유자 NetworkTransform 예제와 다릅니다. 통신 판정의 신뢰 경계를 설명하기 쉽게 하기 위한 선택입니다.
작성 지원 도구를 사용했다는 사실을 숨기지 말고 제출자가 코드 동작을 설명할 수 있도록 직접 실행/수정하며 학습해야 합니다.
