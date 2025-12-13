# 기여 가이드 (Contributing Guide)

ZenECS 프로젝트에 기여해 주셔서 감사합니다! 이 문서는 프로젝트에 기여하는 방법을 안내합니다.

## 목차

- [행동 강령](#행동-강령)
- [기여 방법](#기여-방법)
- [개발 환경 설정](#개발-환경-설정)
- [작업 흐름](#작업-흐름)
- [코드 스타일](#코드-스타일)
- [커밋 메시지](#커밋-메시지)
- [Pull Request](#pull-request)
- [테스트](#테스트)
- [문서화](#문서화)
- [질문하기](#질문하기)

---

## 행동 강령

이 프로젝트는 모든 기여자에게 환영받는 환경을 제공하기 위해 노력합니다. 모든 참여자는 [행동 강령](Docs/community/code-of-conduct.md)을 준수해야 합니다.

**핵심 원칙:**
- 서로를 존중하세요
- 건설적인 피드백을 제공하세요
- 다양한 관점과 경험을 환영합니다

---

## 기여 방법

다음과 같은 방법으로 프로젝트에 기여할 수 있습니다:

### 🐛 버그 리포트

버그를 발견하셨나요? 다음 정보를 포함하여 [이슈를 생성](https://github.com/Pippapips/ZenECS/issues/new)해 주세요:

- **버그 설명**: 무엇이 잘못되었는지 명확하게 설명
- **재현 단계**: 버그를 재현하는 방법
- **예상 동작**: 기대했던 동작
- **실제 동작**: 실제로 발생한 동작
- **환경 정보**: OS, .NET 버전, Unity 버전 (해당 시)
- **스크린샷/로그**: 가능한 경우 첨부

### 💡 기능 제안

새로운 기능을 제안하고 싶으신가요?

1. 먼저 [이슈를 생성](https://github.com/Pippapips/ZenECS/issues/new)하여 논의를 시작하세요
2. 큰 변경사항의 경우, 구현 전에 설계를 논의하는 것이 좋습니다
3. 기존 이슈를 확인하여 중복을 방지하세요

### 📝 문서 개선

문서 개선도 중요한 기여입니다:

- 오타 수정
- 예제 추가
- 설명 개선
- 번역 추가

### 🔧 코드 기여

코드 기여를 시작하기 전에:

1. **작은 변경사항**: 바로 PR을 생성해도 됩니다
2. **큰 변경사항**: 먼저 이슈를 생성하여 설계를 논의하세요
3. **핵심 기능 변경**: 메인테이너와 먼저 상의하세요

---

## 개발 환경 설정

### 필수 요구사항

- **.NET SDK**: .NET Standard 2.1 이상
- **Unity** (선택): Unity 2021.3 이상 (어댑터 개발 시)
- **Git**: 최신 버전
- **에디터**: Visual Studio, Rider, 또는 VS Code

### 저장소 클론

```bash
git clone https://github.com/Pippapips/ZenECS.git
cd ZenECS
```

### 프로젝트 구조

```
ZenECS/
├── Packages/
│   ├── com.zenecs.core/          # Core 패키지 (UPM)
│   └── com.zenecs.adapter.unity/ # Unity 어댑터 (UPM)
├── src/                           # NuGet 빌드용 소스
├── Docs/                          # 문서
└── scripts/                       # 빌드/릴리스 스크립트
```

### 빌드 및 테스트

#### Core 패키지 빌드

```bash
# .NET 프로젝트 빌드
cd src
dotnet build
```

#### Unity 프로젝트에서 테스트

1. Unity 프로젝트 생성
2. `Packages/manifest.json`에 로컬 패키지 경로 추가:

```json
{
  "dependencies": {
    "com.zenecs.core": "file:../Packages/com.zenecs.core"
  }
}
```

---

## 작업 흐름

### 1. 이슈 확인 및 할당

- 작업할 이슈를 선택하거나 새로 생성합니다
- 큰 변경사항의 경우 먼저 이슈에서 논의합니다

### 2. 브랜치 생성

```bash
# 최신 main 브랜치로 업데이트
git checkout main
git pull origin main

# 새 기능 브랜치 생성
git checkout -b feature/your-feature-name
# 또는
git checkout -b fix/your-bug-fix
```

**브랜치 네이밍 규칙:**
- `feature/` - 새 기능
- `fix/` - 버그 수정
- `docs/` - 문서 변경
- `refactor/` - 리팩토링
- `test/` - 테스트 추가/수정

### 3. 코드 작성

- [코드 스타일 가이드](STYLE_GUIDE.md)를 따릅니다
- 테스트를 작성합니다 (가능한 경우)
- 문서를 업데이트합니다

### 4. 커밋

[커밋 메시지 규칙](#커밋-메시지)을 따릅니다.

### 5. Push 및 PR 생성

```bash
git push origin feature/your-feature-name
```

그 다음 GitHub에서 Pull Request를 생성합니다.

---

## 코드 스타일

자세한 내용은 [STYLE_GUIDE.md](STYLE_GUIDE.md)를 참조하세요.

**핵심 원칙:**
- C# 코딩 컨벤션 준수
- XML 문서 주석 작성
- `#nullable enable` 사용
- `readonly struct` 및 `sealed class` 적극 활용
- 파일 헤더 주석 포함

---

## 커밋 메시지

이 프로젝트는 **Conventional Commits** 형식을 사용합니다.

### 형식

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Type

- `feat`: 새로운 기능
- `fix`: 버그 수정
- `docs`: 문서 변경
- `style`: 코드 포맷팅 (기능 변경 없음)
- `refactor`: 리팩토링
- `test`: 테스트 추가/수정
- `chore`: 빌드 프로세스 또는 보조 도구 변경

### 예시

```bash
feat(core): Add entity generation validation

Add generation-based validation to prevent stale entity references.
This improves safety when working with entities across frames.

Closes #123
```

```bash
fix(adapter): Fix Unity editor crash on domain reload

The adapter was not properly handling Unity's domain reload, causing
crashes when entering play mode multiple times.

Fixes #456
```

```bash
docs(api): Update IWorld interface documentation

Add missing examples for query methods and clarify filter usage.
```

### 규칙

- 제목은 50자 이내로 작성
- 첫 글자는 대문자로 시작
- 마지막에 마침표 사용하지 않음
- 본문은 72자마다 줄바꿈
- 본문은 "무엇을"과 "왜"를 설명 (어떻게는 코드에서 명확)
- 이슈 번호 참조: `Closes #123`, `Fixes #456`

---

## Pull Request

### PR 체크리스트

PR을 제출하기 전에 다음을 확인하세요:

- [ ] 코드가 [스타일 가이드](STYLE_GUIDE.md)를 따릅니다
- [ ] 커밋 메시지가 Conventional Commits 형식을 따릅니다
- [ ] 테스트를 추가하거나 수정했습니다 (해당 시)
- [ ] 문서를 업데이트했습니다 (API 변경 시)
- [ ] [CHANGELOG.md](Docs/release/changelog.md)를 업데이트했습니다
- [ ] 빌드가 성공합니다
- [ ] 기존 테스트가 통과합니다

### PR 설명서

PR 설명에는 다음을 포함하세요:

1. **변경 사항 요약**: 무엇을 변경했는지
2. **변경 이유**: 왜 이 변경이 필요한지
3. **테스트 방법**: 어떻게 테스트했는지
4. **관련 이슈**: 연결된 이슈 번호

### 예시

```markdown
## 변경 사항
- Entity 생성 시 generation 검증 추가
- Stale reference 방지를 위한 안전장치 구현

## 변경 이유
기존 코드에서 삭제된 엔티티를 참조하는 버그가 발견되었습니다.
Generation 기반 검증을 추가하여 이를 방지합니다.

## 테스트
- [x] 단위 테스트 추가
- [x] 통합 테스트 통과
- [x] Unity 에디터에서 수동 테스트 완료

## 관련 이슈
Closes #123
```

### 리뷰 프로세스

1. **자동 검사**: CI가 빌드 및 테스트를 실행합니다
2. **코드 리뷰**: 메인테이너가 코드를 검토합니다
3. **피드백 반영**: 요청된 변경사항을 반영합니다
4. **병합**: 승인 후 메인 브랜치에 병합됩니다

---

## 테스트

### 테스트 작성

가능한 경우 테스트를 작성하세요:

```csharp
[Test]
public void Entity_Creation_GeneratesValidHandle()
{
    // Arrange
    var kernel = new Kernel();
    var world = kernel.CreateWorld("Test");

    // Act
    var entity = world.CreateEntity();

    // Assert
    Assert.IsTrue(entity.IsValid);
    Assert.IsTrue(world.IsAlive(entity));
}
```

### 테스트 실행

```bash
# .NET 테스트
cd src
dotnet test
```

---

## 문서화

### 코드 문서화

모든 public API는 XML 문서 주석을 포함해야 합니다:

```csharp
/// <summary>
/// Creates a new entity in this world.
/// </summary>
/// <returns>A new entity handle.</returns>
/// <remarks>
/// The entity is immediately available for component operations.
/// </remarks>
public Entity CreateEntity() { ... }
```

### 문서 업데이트

다음 경우 문서를 업데이트해야 합니다:

- 새로운 API 추가
- 기존 API 변경
- 동작 변경
- 예제 추가/수정

문서 위치:
- API 문서: `Docs/core/api-reference.md`
- 가이드: `Docs/guides/`
- 샘플: `Packages/com.zenecs.core/Samples~/`

### CHANGELOG 업데이트

모든 변경사항은 [CHANGELOG.md](Docs/release/changelog.md)에 기록해야 합니다.

**Keep a Changelog** 형식을 따릅니다:

```markdown
## [Unreleased]

### Added
- Entity generation validation

### Changed
- Improved query performance

### Fixed
- Memory leak in component pooling
```

---

## 질문하기

질문이 있으신가요?

- **이슈 생성**: 기술적 질문이나 논의는 [이슈](https://github.com/Pippapips/ZenECS/issues)에서 진행
- **문서 확인**: [문서](Docs/)를 먼저 확인해보세요
- **FAQ**: [FAQ](Docs/overview/faq.md)를 확인하세요

---

## 라이선스

기여하신 코드는 프로젝트의 MIT 라이선스 하에 배포됩니다.

---

## 감사합니다!

ZenECS 프로젝트에 기여해 주셔서 감사합니다. 여러분의 기여가 프로젝트를 더 나은 방향으로 이끕니다! 🎉

