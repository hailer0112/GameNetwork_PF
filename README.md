# 실행 안내

## 1. 실행 환경

| 항목 | 버전 |
|---|---|
| Unity Editor | 6000.6.0f1 |
| Netcode for GameObjects | 2.13.3 |
| Unity Transport | 6.6.0 |
| Input System | 1.20.0 |
| uGUI | 2.6.0 |
| UniTask | 커밋 `a77a1b4a89824fb6df293099de3060daf2a432c1` |
| .NET SDK | 기존 빌드 확인 환경: 10.0.400 |
| .NET 대상 프레임워크 / 실행 Runtime | net8.0 / 기존 실행 확인 환경: 8.0.28 |

- Windows 기준입니다. Unity에서 Windows 빌드를 지원하는 모듈이 필요합니다.
- Unity 패키지는 `Packages/manifest.json`과 `Packages/packages-lock.json`을 통해 복원됩니다.
- UniTask의 Git 패키지를 복원하려면 Git 클라이언트와 네트워크 연결이 필요합니다.
- UDP 프로그램은 .NET SDK와 .NET 8 Runtime을 준비합니다.

## 2. 시작 위치

- Unity Hub에 추가할 폴더: **클론한 `GameNetwork_PF` 폴더 자체**.
- 시작 씬: Unity 아래쪽 Project 창에서 **Assets → Scenes → CoinArena** 더블클릭.
- UDP 콘솔 프로젝트: `UdpPeer/UdpPeer.csproj`.
- 아래 터미널 명령은 모두 `GameNetwork_PF` 폴더에서 실행합니다.

## 3. Unity 빌드 및 Host·Client 실행 순서

1. Unity Hub에서 `GameNetwork_PF`를 추가하고 Unity 6000.6.0f1로 엽니다.
2. 패키지 복원과 컴파일이 끝나면 **Assets → Scenes → CoinArena**를 더블클릭합니다.
3. 재생 중이라면 위쪽 **▶**를 눌러 재생을 끕니다.
4. Unity 맨 위 메뉴에서 **Coin Link → Build and Run Windows**를 누릅니다.
5. 빌드가 완료되면 별도 게임 창이 열립니다. 아직 Client를 누르지 않습니다.
6. Unity로 돌아와 **▶**를 누르고 Game 화면의 **Host**를 클릭합니다.
7. 별도로 열린 게임 창에서 **Client**를 클릭합니다.

실행 파일은 `Builds/CoinLink.exe`에 생성됩니다. 다음 실행부터는 이 파일을 더블클릭하면 됩니다. 다른 위치로 옮길 때는 실행 파일뿐 아니라 **Builds 폴더 전체**를 함께 옮깁니다.

수동 빌드: **File → Build Profiles → Windows**에서 Scene List의 `Scenes/CoinArena`를 체크하고 **Build**를 누릅니다. 결과는 `Builds` 폴더에 저장합니다. 이 경로로 빌드한 실행 파일 이름은 설정에 따라 다를 수 있습니다.

## 4. 접속 주소·포트 설정

| 대상 | 기본 설정 | 변경 위치 |
|---|---|---|
| Unity Host·Client | `127.0.0.1:7777` | 게임 화면 위쪽 주소·포트 입력칸. 연결 전에 변경하며 양쪽 포트를 동일하게 설정 |
| UDP 피어 A | 자신 `127.0.0.1:9001`, 상대 `127.0.0.1:9002` | 실행 명령의 인자 |
| UDP 피어 B | 자신 `127.0.0.1:9002`, 상대 `127.0.0.1:9001` | 실행 명령의 인자 |

Unity 입력칸 기본값과 Host 수신 주소는 `Assets/Scripts/CoinMenu.cs`에서 설정합니다. 현재 Host 수신 주소는 코드에서 `127.0.0.1`로 고정되어 있어 **같은 컴퓨터의 두 화면으로 시험**합니다. 입력칸의 주소만 바꿔도 다른 컴퓨터 접속이 가능해지는 것은 아닙니다.

## 5. 게임 조작 및 정상 실행 확인

게임 화면을 클릭해 키보드 입력을 활성화합니다.

| 조작 | 기능 / 확인할 결과 |
|---|---|
| WASD 또는 방향키 | 본인 캐릭터 이동 |
| E 또는 Collect | 가장 가까운 남은 코인 획득 요청 |
| Invalid ID | 잘못된 코인 ID 요청. `REJECT invalid ID` 확인 |
| Far coin #11 | 먼 코인 요청. 코인 11이 멀리 있고 미획득일 때 `REJECT out of range` 확인 |
| Repeat last | 직전 요청 반복. 정상 획득 직후 누르면 `already collected` 거절 확인 |
| Disconnect | 연결 종료. Client에서 다시 Client를 눌러 재접속 |

정상 실행 기준:

- 두 화면에 `Connected`가 표시되고 Host 화면에 **players 2**가 표시됩니다.
- 본인 캐릭터는 청록색, 상대는 분홍색이며 상대의 이동이 다른 화면에도 반영됩니다.
- 코인을 획득하면 양쪽의 **Score**가 함께 증가하고 **Left**가 함께 감소합니다.
- 같은 코인에 대한 반복 요청은 점수가 추가되지 않습니다.
- Host를 유지한 채 Client가 재접속하면 현재 점수와 남은 코인이 표시됩니다.

## 6. UDP 피어 빌드 및 실행 순서

터미널 두 개를 열고, 양쪽 모두 `GameNetwork_PF` 폴더로 이동합니다.

먼저 터미널 A에서 빌드한 뒤 피어 A를 실행합니다.

```powershell
dotnet build UdpPeer/UdpPeer.csproj
dotnet run --project UdpPeer/UdpPeer.csproj --no-build -- A 127.0.0.1 9001 127.0.0.1 9002
```

다음으로 터미널 B에서 피어 B를 실행합니다.

```powershell
dotnet run --project UdpPeer/UdpPeer.csproj --no-build -- B 127.0.0.1 9002 127.0.0.1 9001
```

명령 인자 순서는 `피어ID 자신IP 자신포트 상대IP 상대포트`입니다. 포트를 변경하면 양쪽의 자신·상대 포트를 서로 맞춥니다. 두 피어가 모두 READY를 출력한 뒤 다음 조작을 진행합니다.

| 입력 위치 / 명령 | 정상 출력 또는 기능 |
|---|---|
| A: `move 3 5` | B에 `APPLY A (3,5) seq=1` 표시 |
| B: `move 8 2` | A에 `APPLY B (8,2) seq=1` 표시 |
| A: `move 4 6` 후 `resend 1`, `resend 2` | B에 `IGNORE` 표시, A의 최신 위치 (4,6) 유지 |
| `raw nonsense` | 상대에 `REJECT` 표시 |
| `raw POS1\|A\|bad\|x\|y` | 상대에 잘못된 메시지 거절 표시 |
| `raw ` (뒤에 공백 하나 포함) | 빈 UDP 메시지 송신, 상대에서 거절 |
| `load 100 10` | 100개를 10ms 간격으로 송신 |
| `load 100 0` / `load 1000 0` | 100개 / 1000개를 대기 간격 없이 송신 |
| `status` | SELF·REMOTE 위치와 송수신·거절·무시 횟수 출력 |
| `quit` | 종료 |

시작 시 `READY A` / `READY B`가 표시됩니다. 최초 `REMOTE: not received`는 아직 상대 메시지를 받지 않았다는 뜻입니다. 이동 메시지를 보낸 뒤 상대의 APPLY와 status의 REMOTE 좌표를 확인합니다.

자동 시험은 PowerShell 7에서 실행합니다.

```powershell
pwsh -File ./Tools/Test-Udp.ps1
pwsh -File ./Tools/Test-Coin.ps1
```

게임 자동 시험은 먼저 `Builds/CoinLink.exe`를 빌드해야 합니다. UDP 자동 시험은 19001·19002 포트를 사용합니다.

## 7. 알려진 오류·미완성 기능·환경 제한

- **Builds 폴더가 없음:** 실행 파일은 Git에서 제외되어 있습니다. 최초 클론 후 위 빌드 절차를 수행합니다.
- **Assets가 비어 있음:** Unity Hub에서 연 폴더가 `Assets`, `Packages`, `ProjectSettings`를 바로 포함하는 `GameNetwork_PF`인지 확인합니다.
- **Coin Link 메뉴가 없음:** 패키지 복원·컴파일 완료 여부와 Console의 컴파일 오류를 확인합니다.
- **UniTask 호환성:** 기존 시험에서 2.5.10의 Editor TreeView 컴파일 오류가 있어 위 커밋으로 고정했습니다.
- **Unity 버전:** 프로젝트는 6000.6.0f1 기준입니다. 수업 검증 환경의 6000.6.5f1과 다르며, 동일한 실행 결과를 보장하지 않습니다.
- **접속 실패:** Host를 먼저 시작하고 양쪽 주소·포트를 확인합니다. 연결 시도는 약 10초 후 시간 초과되며 다시 시도할 수 있습니다.
- **UDP Socket error:** 같은 포트를 사용하는 다른 프로그램이나 남아 있는 피어 프로세스가 있는지 확인합니다.
- **UDP 피어 재시작:** 순서 번호 복구를 지원하지 않으므로 시험을 다시 시작할 때 두 피어를 모두 종료하고 재실행합니다.
- **게임 범위:** 같은 컴퓨터에서 2명 연결을 시험하는 용도입니다. 인터넷 매칭·인증·계정·암호화·DB 저장은 구현하지 않았습니다.
- **세션 유지:** Host 종료 시 세션이 끝납니다. 새 Host 세션에서는 점수와 코인이 초기화됩니다.
- **추가 확인 필요:** 다양한 창 크기의 UI 배치와 정확히 동시인 코인 획득 경쟁은 추가 수동 확인 대상입니다.
