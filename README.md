# Coin Link — 게임 네트워크 과제

하나의 제출 묶음에 Unity 코인 게임과 독립적인 UDP 콘솔 프로그램이 있습니다.
강사님 요구사항: https://github.com/Unity-Program-Lecture/csharp_basic/blob/main/Documents/Curriculum/10.GameNetwork/PROJECT_SUBMISSION_GUIDE.md


## 저장소 구성

이 저장소 자체가 제출 폴더입니다. GitHub에서 내려받은 뒤 아래 구조를 기준으로 실행하세요.

```text
GameNetwork_PF/
  README.md
  DESIGN.md
  TEST_REPORT.md
  Assets/         게임 씬·스크립트·프리팹
  Packages/       Unity 패키지 설정
  ProjectSettings/ Unity 프로젝트 설정
  Builds/         로컬에서 생성하는 실행 파일
  UdpPeer/        .NET 콘솔 프로젝트
  Evidence/       시험 기록
  Tools/          재현용 자동 시험 스크립트
```

Unity의 Library/Temp/Logs/Builds와 .NET의 bin/obj는 GitHub 제출에서 제외합니다. 클론한 GameNetwork_PF 폴더 자체를 Unity Hub에서 열면 됩니다. CoinGame을 별도로 클론하거나 하위 프로젝트를 찾을 필요가 없습니다. 실행 파일은 소스와 별개이며 최초 한 번 빌드해야 합니다.

## 기존 클론을 업데이트할 때

Unity를 닫고 GitHub Desktop에서 Pull origin을 실행한 뒤 Unity Hub에 GameNetwork_PF 폴더 자체를 추가하세요. 기존 CoinGame/Builds에 만든 실행 파일은 자동 이동하지 않습니다. 필요하면 해당 Builds 폴더 전체를 저장소 최상위로 복사하세요. 실행 파일만 따로 옮기지 말고 데이터 폴더와 DLL도 함께 유지하세요. 기존 파일과 충돌해 Pull이 멈추면 강제로 덮어쓰지 말고 GitHub Desktop에 표시되는 경로를 확인하세요.

## 개발 환경

- 실제 개발 Editor: Unity 6000.6.0f1 (사용자 PC에 설치된 버전). 강사님 검증 프로젝트의 6000.6.5f1과 다릅니다.
- NGO 2.13.3 / Unity Transport 6.6.0 / Input System 1.20.0 / uGUI 2.6.0.
- 패키지는 Packages의 manifest.json 및 packages-lock.json으로 복원합니다.
- 렌더링: 기본 렌더 파이프라인. 추가 그래픽 리소스는 없습니다.
- UDP: net8.0. 개발 PC의 .NET SDK 10.0.400으로 빌드하고 .NET Runtime 8.0.28로 실행.
- UniTask: 공식 커밋 a77a1b4a89824fb6df293099de3060daf2a432c1로 고정했습니다. 2.5.10 태그는 현재 Unity의 Editor TreeView API에서 컴파일 오류가 있어 호환 수정이 있는 공식 소스로 변경했습니다. 이는 수업 지정 버전으로 확인된 값은 아닙니다. 연결 완료/실패를 취소 가능한 비동기 대기로 처리하고 시간 제한은 Update에서 관리합니다. Git 클라이언트가 있어야 UPM Git 패키지를 복원할 수 있습니다.

## Unity 실행

1. GameNetwork_PF를 클론하고 Unity Hub에서 **클론한 GameNetwork_PF 폴더 자체**를 프로젝트로 추가합니다.
2. 6000.6.0f1로 열고 패키지 복원/컴파일 완료를 기다립니다.
3. Assets/Scenes/CoinArena.unity를 엽니다. 씬이 없다면 Coin Link > Create Arena로 생성합니다. 생성 도구는 기존 씬을 덮어쓰지 않습니다.
4. Play를 누르고 Host를 클릭합니다. 기본 주소 127.0.0.1, 포트 7777.
5. Unity 상단 **Coin Link > Build and Run Windows**를 누르면 Builds/CoinLink.exe를 만들고 실행합니다. 다음부터는 Builds/CoinLink.exe를 직접 실행해도 됩니다.
6. 실행 파일을 열고 Client를 클릭합니다. Host 화면에 players 2가 표시되어야 합니다.

조작: WASD/방향키 이동, E 또는 Collect 버튼으로 가장 가까운 남은 코인 획득 요청.
본인 캐릭터는 청록색, 상대는 분홍색입니다. 화면의 Score는 공동 점수이며 Left는 남은 코인 수입니다.
주소/포트는 연결 전 입력칸에서 변경합니다. Host 수신은 로컬 127.0.0.1로 제한합니다.
연결 중 시작 버튼은 비활성화되고, 실패/10초 시간 제한 후 다시 시도할 수 있습니다.

시험 버튼:
- Invalid ID: -1을 요청하여 서버 거절을 확인합니다.
- Far coin #11: 시작 위치에서 먼 코인 11을 요청합니다. 먼저 그 코인을 획득하거나 접근했다면 거리 시험 조건이 달라집니다.
- Repeat last: 직전 요청을 반복합니다. 정상 획득 후 사용하면 이미 획득한 코인 거절을 확인합니다.
- 두 참여자가 같은 코인 가까이에 선 뒤 E를 누르면 공동 점수는 한 번만 증가해야 합니다.

Client에서 Disconnect 후 Client를 다시 누르면 현재 점수/남은 코인이 동기화됩니다.
Host가 종료되면 세션이 끝나고, 새 Host 세션은 초기화됩니다. 개인 점수/DB/계정 저장은 없습니다.

자동 빌드: Unity 실행 파일에 -batchmode -nographics -quit -projectPath <GameNetwork_PF 경로> -executeMethod CoinGameBuilder.BuildBatch -logFile <로그 경로> 인자를 전달합니다.
빌드 후 PowerShell에서 `./Tools/Test-Coin.ps1`을 실행하면 두 별도 Player의 연결/서버 판정/이동/재접속을 시험합니다.
이 자동 시험은 UI 마우스 클릭 검증과 구분합니다.

## UDP 실행

터미널을 두 개 열고 이 문서가 있는 폴더에서 실행합니다.

```text
dotnet build UdpPeer/UdpPeer.csproj
dotnet run --project UdpPeer/UdpPeer.csproj --no-build -- A 127.0.0.1 9001 127.0.0.1 9002
```

다른 터미널:

```text
dotnet run --project UdpPeer/UdpPeer.csproj --no-build -- B 127.0.0.1 9002 127.0.0.1 9001
```

명령:
- A에서 `move 3 5`, B에서 `move 8 2`: 상대 위치가 교차 표시됩니다.
- A에서 `move 4 6` 후 `resend 1`, `resend 2`: B는 IGNORE를 표시하고 최신 위치를 유지합니다.
- `raw nonsense`, `raw POS1|A|bad|x|y`: 잘못된 메시지 거절.
- `raw `: 공백 뒤 아무 내용 없이 입력하면 빈 UDP 메시지를 보냅니다.
- `load 100 10`, `load 100 0`, `load 1000 0`: 개수/전송 간격(ms) 조절.
- `status`: 자기/상대 위치와 송수신 카운터. `quit`: 정상 종료.

전송량 자동 시험: PowerShell에서 `./Tools/Test-Udp.ps1`.
9001/9002는 직접 실행용, 19001/19002는 자동 시험용입니다. 프로그램 시작 인자로 변경할 수 있습니다.
순서 번호는 좌표가 바뀌어 새 위치 메시지를 만들 때 증가합니다. resend는 기존 번호/좌표를 그대로 사용합니다.
프로세스 재시작 시 번호 복구는 지원하지 않으므로 시험 재시작 때 두 피어를 함께 재시작합니다.

## 현재 상태/제한

실제 검증 결과와 미검증 항목은 TEST_REPORT.md를 확인하세요.
로컬 2명 학습용이며 인터넷 매칭, 암호화, 인증, 계정, 데이터 영속 저장은 구현하지 않았습니다.
UI는 영문이며 그래픽 대신 통신/서버 판정 확인을 우선합니다.
저장소 공개 여부와 제출 기한은 별도 공지를 확인해야 합니다.

## 제출 전

저장소 루트의 Assets/.meta, Packages, ProjectSettings와 UdpPeer의 .csproj/소스, 본 문서, DESIGN.md, TEST_REPORT.md, Evidence를 포함하세요.
Library/Temp/Logs/UserSettings/Builds, UdpPeer/bin/obj는 제외합니다.
이 폴더 전체를 다른 위치로 복사하여 안내서대로 재실행하세요. GitHub 제출 시 평가자가 접근할 수 있는 공개 저장소가 필요합니다.



