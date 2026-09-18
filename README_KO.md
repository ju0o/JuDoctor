# JuDoctor

> **컴퓨터가 왜 느린지, 하드웨어를 바꾸기 전에 근거부터 확인합니다.**

[English README](README.md) · [Releases](https://github.com/ju0o/JuDoctor/releases) · [알려진 문제](KNOWN_ISSUES.md) · [개인정보](PRIVACY.md)

JuDoctor는 Windows에서 조용히 백그라운드로 동작하면서 **CPU · RAM · GPU · VRAM · 디스크 상태를 기록하고, 반복되는 실제 병목을 찾아 설명하는 로컬 우선 PC 진단 프로그램**입니다.

작업 관리자가 숫자를 보여준다면, JuDoctor는 그 다음 질문에 답하려고 합니다.

> **“그래서 내 PC를 진짜 업그레이드해야 하나? 근거는 뭔데?”**

---

## 다운로드

### Windows 10 / 11 x64

**[⬇ JuDoctor 다운로드 페이지](https://github.com/ju0o/JuDoctor/releases)**

현재 이 저장소에는 V1 소스가 공개되어 있고, 첫 공개 설치본 `v1.0.0`을 준비하고 있습니다. Release가 발행되면 아래 주소가 최신 안정판으로 연결됩니다.

**[최신 안정판](https://github.com/ju0o/JuDoctor/releases/latest)**

공식 Windows Release에는 다음 파일을 제공합니다.

- `JuDoctor-<version>-Setup.exe`
- `JuDoctor-<version>-Setup.exe.sha256`

V1은 self-contained 방식이라 별도 .NET Runtime 설치가 필요하지 않습니다.

> 초기 unsigned 배포본은 Windows SmartScreen에서 알려지지 않은 게시자로 표시될 수 있습니다. 같은 Release에 공개된 SHA256과 설치 파일의 해시를 비교해 주세요.

자세한 설치 방법은 [설치 문서](docs/INSTALLATION.md)를 참고하세요.

---

## 주요 기능

- Windows 트레이 백그라운드 상주
- CPU / RAM / GPU / VRAM / Disk 모니터링
- 짧은 순간값이 아닌 지속 시간과 복수 신호 기반 판단
- **Why Was My PC Slow?** 최근 느려진 원인 분석
- Incident History
- 로컬 SQLite 기록
- 최소 14일 관찰 후 CPU/RAM 업그레이드 권장 가능
- Windows 자동 시작
- 중복 프로세스 실행 방지
- 계정 불필요
- 클라우드 불필요
- V1 원격 텔레메트리 업로드 없음

JuDoctor는 CPU가 잠깐 100%가 됐다는 이유만으로 CPU 교체를 권하지 않습니다. GPU가 게임 중 99%라고 해서 경고를 남발하지도 않습니다.

---

## 왜 작업 관리자만으로는 부족한가?

| 상황 | 작업 관리자 | JuDoctor |
| --- | --- | --- |
| 빌드 중 CPU 100% | 숫자 표시 | 짧은 정상 부하는 대부분 무시 |
| RAM 부족 반복 | 현재 사용량만 확인 | 여러 작업 세션의 반복 압박 기록 |
| 15분 전에 PC가 느렸음 | 이미 지나간 상태 | 최근 기록을 기반으로 원인 확인 |
| RAM을 늘릴지 고민 | 직접 판단 | 충분한 장기 근거가 있을 때만 권장 |
| 게임 중 GPU 99% | 99% 표시 | 이것만으로 교체 권장하지 않음 |

---

## V1 진단 흐름

```text
백그라운드 관찰
      ↓
이상 징후 후보
      ↓
복수 신호 + 지속 시간 확인
      ↓
Incident
      ↓
진단 / 기록
      ↓
필요한 경우에만 알림
      ↓
장기 증거 축적
      ↓
CPU / RAM 업그레이드 판단
```

CPU/RAM 업그레이드 권장은 **최소 14일 관찰**이 선행되어야 합니다. 14일이 지났다고 자동으로 권장이 뜨는 것은 아닙니다.

---

## 개인정보

JuDoctor V1은 수집한 상태 기록을 사용자 PC에 보관합니다.

다음과 같은 내용은 수집할 필요가 없도록 설계했습니다.

- 파일 내용
- 브라우저 페이지 내용
- 터미널/명령어 텍스트
- 비밀번호
- 클립보드
- 개인 문서 내용

자세한 내용은 [PRIVACY.md](PRIVACY.md)를 참고하세요.

---

## V1 검증 상태

V1 Release Candidate는 내부 인증에서 다음을 통과했습니다.

- 자동화 테스트 **93 / 93 PASS**
- Founder 수동 체크 **15 / 15 PASS**
- RTX 3050 계열에서 DXGI 전용 VRAM 약 5.9 GB 실측
- 재부팅 후 tray-only 자동 시작
- DB 지속 기록 및 재시작/재부팅 복구
- Resource Governor 비상/복귀 모드
- CPU/RAM Level 4 추천 알림 스케줄 및 재시작 후 중복 방지

최종 내부 인증 상태:

`USER_STABLE_PASS_WITH_KNOWN_ISSUES`

현재 공개 저장소의 `main`을 기준으로 첫 Release 전 재검증 중이며 진행 상황은 [Issue #1](https://github.com/ju0o/JuDoctor/issues/1)에서 확인할 수 있습니다.

---

## 화면

V1에는 다음 화면이 있습니다.

- Dashboard
- Incident History
- Why Was My PC Slow?
- System Capacity
- Settings

첫 공개 바이너리 Release 전에 실제 스크린샷을 README에 추가할 예정입니다.

---

## 소스에서 빌드하기

V1 소스는 현재 이 Public 저장소에 공개되어 있습니다.

```text
src/        애플리케이션 소스
tests/      unit / integration / storage 테스트
installer/  self-contained publish + Inno Setup
planning/   제품/아키텍처 문서
qa/         인증 보고서와 QA 도구
```

JuDoctor는 .NET 9 기반 Windows WPF 애플리케이션입니다. `MainPCDoctor.*` 같은 내부 프로젝트/namespace 이름은 이미 인증된 V1을 흔들지 않기 위해 유지하고, 사용자에게 보이는 제품 브랜드만 **JuDoctor**로 사용합니다.

```powershell
# [저장소 루트]
dotnet restore MainPCDoctor.sln
dotnet test MainPCDoctor.sln --configuration Release
.\installer\publish.ps1 -Installer
```

V1에서는 `PublishSingleFile=true`를 사용하지 않습니다. WPF pack-URI 관련 알려진 제약 때문에 self-contained folder publish를 installer로 감싸는 방식이 정식 배포 방식입니다.

---

## 오픈소스 / 라이선스

JuDoctor는 **GNU GPL v3.0** 공개를 준비하고 있습니다. [LICENSE](LICENSE)

첫 바이너리 배포 전 canonical GPL-3.0 전문을 저장소에 포함하는 작업은 [Issue #1](https://github.com/ju0o/JuDoctor/issues/1)의 Release blocker입니다.

`JuDoctor` 이름과 공식 브랜드를 제3자가 자신의 배포본에 공식 제품처럼 사용하는 권리는 별도입니다. [TRADEMARKS.md](TRADEMARKS.md)

---

## 문제 제보

하드웨어 호환성 제보가 특히 도움이 됩니다.

- [버그 제보](https://github.com/ju0o/JuDoctor/issues/new?template=bug_report.yml)
- [기능 제안](https://github.com/ju0o/JuDoctor/issues/new?template=feature_request.yml)

로그나 DB를 첨부하기 전에는 반드시 개인정보가 포함되지 않았는지 직접 확인하세요.

---

**JuDoctor V1 — Windows / Local-first / Public release track**

Public source migration은 완료되었습니다. 현재는 공개 QA 자료 정리, 사용자 노출 브랜드 정리, public `main` 재검증, `v1.0.0` 설치본 배포가 남아 있습니다.

> 작업 관리자는 숫자를 보여줍니다. **JuDoctor는 그 숫자가 진짜 문제인지 판단하려고 합니다.**
