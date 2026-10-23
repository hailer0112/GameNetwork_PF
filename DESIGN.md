# 설계 설명

## 1. 게임 요구사항과 Host·Client의 역할

같은 컴퓨터에서 Host와 Client를 실행하여 서로의 이동, 코인 획득, 공동 점수와 남은 코인 상태를 확인하는 코인 게임입니다. 잘못된 코인 ID, 거리 밖의 획득 요청, 이미 획득한 코인의 반복 요청은 서버가 거절하며, 재접속한 Client는 진행 중인 세션의 상태를 받습니다. 별도 UDP 콘솔 프로그램에서는 두 피어가 위치를 교환하고 오래된 메시지와 비정상 메시지를 처리합니다.

| 구분 | 역할 |
|---|---|
| Host | 서버와 본인 Client를 함께 실행. 모든 플레이어의 이동 계산, 코인 획득 판정, 공동 상태 변경 담당 |
| Client | 본인 플레이어의 이동 입력과 코인 ID를 서버에 요청. 서버가 전달한 위치·점수·코인 상태를 화면에 표시 |
| UDP 피어 A·B | 각각 송신자이자 수신자로 동작. 상대 위치 메시지를 검증하고 최신 위치를 유지 |

게임 Client가 점수나 획득 여부를 직접 확정하지 않도록 서버에 최종 판정 권한을 둡니다.

## 2. 코인 게임 구성과 데이터 흐름

| 구성 요소 | 담당 기능 |
|---|---|
| CoinMenu | 접속·종료, 주소·포트 입력, 상태 및 판정 결과 표시, 시험 버튼 |
| CoinPlayer | 소유자의 입력 수집, 이동·획득 RPC, 서버 이동 계산, 위치 동기화, 개인 응답 수신 |
| CoinRules | 코인 수·ID별 고정 위치·획득 가능 거리 |
| CoinSession | 공동 점수, 획득 비트마스크, 승인·거절 횟수, 서버 획득 판정 |
| NetworkManager / UnityTransport | 접속 관리와 NGO 메시지 전송 |
| CoinGameBuilder | Editor에서 씬·프리팹 생성 및 Windows 빌드 |
| CoinSmoke | 실행 인자를 지정했을 때 동작하는 자동 통합 시험 |

### 이동 흐름

1. 소유자 CoinPlayer가 이동 입력을 읽고 50ms마다 MoveServerRpc를 보냅니다.
2. 서버가 입력의 NaN·Infinity를 거절하고 입력 벡터 길이를 최대 1로 제한합니다.
3. 서버 CharacterController가 이동과 충돌을 처리합니다. 300ms 동안 입력 갱신이 없으면 이동 입력을 0으로 처리합니다.
4. 서버가 Position의 NetworkVariable 값을 갱신합니다.
5. 각 Client가 동기화된 위치를 받아 보간하여 표시합니다.

### 코인 획득 흐름

1. E 또는 Collect로 가까운 미획득 코인을 선택하거나 시험 버튼으로 특정 ID를 지정합니다.
2. 소유자 CoinPlayer가 CollectServerRpc로 코인 ID를 보냅니다.
3. 서버 CoinSession이 ID·중복·거리를 검사합니다.
4. 승인 시 획득 비트와 공동 점수·승인 횟수를 변경합니다. 거절 시 거절 횟수를 증가시킵니다.
5. NetworkVariable로 공동 상태를 모든 Client에 전달합니다.
6. ReplyClientRpc로 요청한 플레이어의 소유자에게만 ACCEPT 또는 REJECT 결과를 보내 UI에 표시합니다.

## 3. 자료 출처와 기술·통신 방식의 선택 이유

| 출처 또는 기술 | 참고 범위 / 선택 이유 |
|---|---|
| [수업 저장소](https://github.com/Unity-Program-Lecture/csharp_basic) | 기존 설계 기록 기준으로 DAY05/08의 UDP·순서 번호, DAY09의 접속·CharacterController, DAY10/11의 NetworkVariable·RPC·서버 판정, DAY12의 연결 수명주기, DAY14/15의 검증 원칙 참고 |
| [게임 네트워크 제출 안내](https://github.com/Unity-Program-Lecture/csharp_basic/blob/main/Documents/Curriculum/10.GameNetwork/PROJECT_SUBMISSION_GUIDE.md) | 게임과 UDP 프로그램의 제출·설명·시험 범위 참고 |
| Netcode for GameObjects / Unity Transport | Unity 오브젝트의 접속·소유권·RPC·상태 동기화를 일관된 방식으로 처리 |
| ServerRpc / ClientRpc | 입력·획득 요청은 서버로, 요청별 판정 응답은 해당 소유자에게 전달 |
| NetworkVariable | 점수·코인·위치처럼 이후 접속자도 현재 값을 받아야 하는 상태 공유 |
| 서버 CharacterController | 서버가 실제 위치를 계산하여 Client가 보낸 임의 좌표에 의존하지 않고 획득 거리를 판정 |
| [UniTask 고정 커밋](https://github.com/Cysharp/UniTask/commit/a77a1b4a89824fb6df293099de3060daf2a432c1) | 접속 완료·실패를 취소 가능한 비동기 대기로 처리. 기존 시험의 2.5.10 Editor 호환 오류를 피한 커밋 사용 |
| .NET UdpClient / UTF-8 텍스트 | Unity 게임과 독립적으로 datagram 송수신·파싱·순서 관리를 관찰. 구분자 텍스트로 메시지 내용을 직접 확인 |
| 비동기 UDP 수신 작업 | 상대가 전송하지 않는 동안에도 콘솔 입력과 송신이 가능하도록 수신 대기와 입력 루프 분리 |

게임의 이동은 서버 권한 방식이며 Client 예측·지연 보상은 구현하지 않았습니다. 독립 UDP 프로그램의 순서 번호 규칙은 최신 위치 선택용이며 신뢰성 전송이나 인증을 제공하지 않습니다.

## 4. 점수·코인 상태와 서버 검사 규칙

| 상태 | 구조 / 권한 |
|---|---|
| Score | 공동 점수. 서버 쓰기 권한의 NetworkVariable<int> |
| CollectedMask | 코인 0~11의 획득 여부를 각 비트로 표현하는 NetworkVariable<int> |
| Accepted / Rejected | 승인·거절 요청 횟수. 서버가 변경하는 NetworkVariable<int> |
| Remaining | CollectedMask에서 미획득 코인 수를 계산하는 값 |
| Position | 플레이어별 서버 쓰기 권한의 NetworkVariable<Vector3> |
| LastReply | 해당 플레이어가 받은 최근 응답 문자열. UI 표시용 로컬 값 |

서버 검사 순서는 다음과 같습니다.

1. 활성 서버 및 스폰된 세션인지 확인합니다.
2. 코인 ID가 0~11 범위인지 확인합니다.
3. 해당 코인의 획득 비트가 이미 설정되어 있는지 확인합니다.
4. 서버가 관리하는 플레이어 위치와 코인 사이 거리가 1.8m 이내인지 확인합니다.

RPC는 기본 소유권 검사를 사용합니다. 승인 시 서버 메인 스레드에서 획득 비트를 먼저 설정한 뒤 점수를 1 증가시킵니다. 같은 코인을 향한 다음 요청은 설정된 비트를 보고 거절하므로 중복 점수를 방지합니다. Client가 선택한 가장 가까운 코인 ID도 서버 검사를 통과해야 합니다.

새 Host 세션은 상태를 0으로 초기화합니다. Host가 유지되는 동안 새 접속자와 재접속자는 현재 NetworkVariable 값을 받습니다.

## 5. UDP 위치 메시지와 최신 위치 관리

위치 데이터의 **네 필드**는 peerId·sequence·x·y입니다. 실제 전송 문자열에는 앞에 규약 표식 POS1이 추가되어 **구분자로 나누면 다섯 항목**입니다.

```text
POS1|peerId|sequence|x|y
POS1|A|12|3|5
```

| 필드 | 형식 / 의미 |
|---|---|
| peerId | ASCII 영숫자·밑줄·하이픈으로 구성한 1~32자 식별자 |
| sequence | 양수 Int64. 새 위치 메시지를 만들 때 증가하는 순서 번호 |
| x | Int32 X 좌표 |
| y | Int32 Y 좌표 |

UTF-8로 인코딩하고 datagram 하나를 메시지 하나로 처리합니다. 수신 시 최대 256바이트, 항목 수, POS1 표식, ID와 숫자 형식을 검사합니다.

- 실행 인자의 자신 IP·포트로 소켓을 열고 상대 IP·포트로 전송합니다.
- 수신 엔드포인트가 설정된 상대와 다르거나 peerId가 자기 ID이면 거절합니다. IP·포트 확인은 암호학적 인증이 아닙니다.
- 피어 ID별 Dictionary에 마지막으로 적용한 Position을 보관합니다.
- 처음 받는 피어이거나 기존보다 sequence가 크면 적용합니다.
- 기존 이하의 sequence는 오래되거나 중복된 메시지로 보고 무시하며 최신 위치를 유지합니다.
- resend는 최근 20,000개 송신 메시지 이력에서 원래 번호·좌표를 그대로 재전송하는 수동 시험 기능입니다. 자동 재전송은 없습니다.
- 프로세스를 재시작하면 순서 번호가 초기화됩니다. 세션 식별·재시작 복구는 없어 시험을 다시 시작할 때 두 피어를 함께 재시작합니다.

## 6. 실시간 상태와 오래 보관할 데이터

| 구분 | 데이터 / 보관 방식 |
|---|---|
| 실시간 게임 상태 | 플레이어 위치·입력, 공동 점수, 코인 비트마스크, 연결 상태. 실행 중 메모리와 NetworkVariable로 관리 |
| 실시간 UDP 상태 | 자기·상대 좌표, 피어별 최신 순서 번호, 카운터, 최근 송신 이력. 프로세스 메모리에서 관리 |
| 오래 보관할 자료 | 소스·설정·설계 설명, 시험 보고서, Evidence의 추출 로그. 파일과 Git 이력으로 보관 |
| 런타임 상세 기록 | 자동 시험에서 Logs에 저장하는 Unity 로그. 제출용 Evidence와 구분하며 Git 제외 |

DB·계정·개인 점수 저장은 없습니다. Host 종료 후 게임 진행 상태, UDP 종료 후 최신 위치와 순서 번호는 복구하지 않습니다. UDP 콘솔 출력도 자동 영구 저장되지 않으며 자동 시험 스크립트가 별도 증거 파일을 만듭니다.

## 7. 접속·종료·오류 등 운영 상태 기록

| 대상 | 현재 표시·기록 항목 |
|---|---|
| 게임 접속 UI | 주소·포트, Connecting·Connected·Disconnected·시간 초과·시작 실패 상태, Host·Client 구분, Host 접속 인원 |
| 게임 상태 UI | 공동 Score·Left, 요청자의 최근 서버 응답 |
| 서버 코인 로그 | 요청자 OwnerClientId, 코인 ID, 승인·거절 사유, 처리 후 점수·비트마스크 |
| Client 응답 로그 | REPLY와 승인·거절 문자열 |
| 자동 게임 시험 로그 | 접속 소유자, 이동 전후 위치·서버 위치, 점수·마스크, 중복 거절, 재접속 결과, 최종 인원·승인·거절 횟수, SMOKE_FAIL |
| 빌드 로그 | COIN_BUILD_OK 또는 빌드 실패 예외 |
| UDP 콘솔 | READY의 자신·상대 엔드포인트, 경과 시간, APPLY·REJECT·IGNORE, SELF·REMOTE 좌표·순서 번호, 송수신·거절·무시 횟수, 부하 송신 수·소요 시간, 소켓·송수신 오류, 종료 시 최종 상태 |

게임의 모든 접속·종료 이벤트를 영구 감사 로그로 남기는 기능은 없습니다. 일반 실행에서는 접속 상태가 UI에 표시되고, 자동 시험에서는 접속·재접속·최종 상태를 별도 기록합니다. UDP 수신 수에는 거절·무시된 datagram도 포함되므로 적용 성공 수와 동일하게 해석하지 않습니다.

## 8. 참고한 코드와 추가·수정한 부분

| 구분 | 범위 |
|---|---|
| 수업 자료 참고 | 접속, CharacterController, NetworkVariable, RPC, 서버 판정, UDP와 순서 번호 및 시험 접근 방식 |
| 기존 프로젝트 사용 | ProjectSettings의 시작점 |
| 새로 작성한 게임 코드 | CoinSession·CoinRules·CoinPlayer·CoinMenu·CoinSmoke·CoinGameBuilder |
| 새로 작성한 UDP·시험 코드 | POS1 메시지 규약, UdpPeer 프로그램, Tools의 게임·UDP 시험 스크립트 |
| 추가·수정한 동작 | 서버 이동 계산, 획득 거리·중복 검사, 요청자별 응답, 접속 대기·시간 제한, 입력칸 편집 중 이동 차단, 상태 기반 자동 이동 시험 |
| 구조·편의 수정 | Assets·Packages·ProjectSettings를 저장소 최상위로 이동, 시험 경로 갱신, Build and Run Windows 메뉴 추가 |

기존 작성 기록에 따르면 강사님의 완성 씬·프리팹·런타임 코드를 복사하지 않고 새로 구성했습니다. 런타임 폰트는 Unity 내장 LegacyRuntime.ttf를 사용합니다. 위 신규 코드·시험 스크립트·문서는 Codex의 작성 지원으로 제작했습니다.
