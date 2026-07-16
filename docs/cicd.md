# CI/CD Pipeline

Sprint 17, Flowist icin otomatik build-test-image-deploy hattini tanimlar.

## CI Workflow

Dosya: `.github/workflows/ci.yml`

CI su durumlarda calisir:

- `main` ve `develop` branch'lerine push yapildiginda
- `main` ve `develop` branch'lerine pull request acildiginda
- GitHub Actions UI uzerinden manuel tetiklendiginde

CI asamalari:

1. Restore: `dotnet restore Flowist.sln`
2. Build: `dotnet build Flowist.sln --configuration Release --no-restore`
3. Lint / format: `dotnet format Flowist.sln --verify-no-changes`
4. Unit tests:
   - AuthService.Tests
   - TaskService.Tests
   - NotificationService.Tests
   - ActivityService.Tests
   - ContractTests
5. Integration tests:
   - AuthService.IntegrationTests
   - TaskService.IntegrationTests
   - NotificationService.IntegrationTests
6. Coverage report:
   - Coverlet collector test sirasinda coverage uretir
   - ReportGenerator bu dosyalari HTML, Cobertura ve GitHub summary raporuna cevirir

Integration testler Testcontainers kullandigi icin GitHub runner uzerinde Docker daemon gerekir. `ubuntu-latest` runner Docker destekledigi icin ek servis tanimina gerek yoktur.

## CD Workflow

Dosya: `.github/workflows/cd.yml`

CD su durumlarda calisir:

- `main` branch'ine push yapildiginda
- `v*.*.*` formatinda tag push yapildiginda
- GitHub Actions UI uzerinden manuel tetiklendiginde

CD asamalari:

1. Docker Buildx hazirlanir.
2. GitHub Container Registry'ye login olunur.
3. Bes image build edilir ve push edilir:
   - `flowist-authservice`
   - `flowist-taskservice`
   - `flowist-notificationservice`
   - `flowist-activityservice`
   - `flowist-apigateway`
4. Deploy job hedef environment'a baglanir:
   - `dev`
   - `staging`
   - `production`
5. `kubectl apply -f k8s/<environment>` calisir.
6. Deployment rollout kontrol edilir.
7. Rollout fail olursa `kubectl rollout undo` ile otomatik rollback denenir.

## Gerekli GitHub Secrets

CD deploy adimi icin repository veya environment secret olarak su deger gerekir:

- `KUBE_CONFIG`: Base64 encoded kubeconfig icerigi.

GHCR push icin ekstra secret gerekmez. Workflow `GITHUB_TOKEN` ve `packages: write` izniyle push yapar.

## Environment Mantigi

Pipeline ortam ayrimini GitHub Environments ile yapar:

- `dev`: Otomatik veya manuel deploy icin kullanilir.
- `staging`: Production oncesi dogrulama ortamidir.
- `production`: Approval gate ile korunmalidir.

GitHub tarafinda `production` environment icin required reviewers acilmalidir.

## Branch Protection Kurallari

T-338 kod dosyasiyla tam uygulanamaz; GitHub repository ayaridir. Onerilen ayar:

- `main` branch protected olmali.
- Pull request zorunlu olmali.
- En az 1 review zorunlu olmali.
- Status checks zorunlu olmali:
  - `Build`
  - `Lint / Format`
  - `Unit Tests`
  - `Integration Tests`
  - `Coverage Report`
- Branch up-to-date olmadan merge edilmemeli.
- Force push kapali olmali.
- Direct push kapali olmali.

## Rollback Stratejisi

T-339 icin strateji:

1. Her image immutable tag ile push edilir: commit SHA veya release tag.
2. Kubernetes deployment rollout sonucu izlenir.
3. Rollout basarisizsa CD workflow otomatik `kubectl rollout undo` calistirir.
4. Production deploy GitHub Environment approval ile korunur.
5. Manuel rollback icin onceki tag secilip CD workflow `workflow_dispatch` ile calistirilabilir.

## Kubernetes Manifest Notu

Workflow `k8s/dev`, `k8s/staging`, `k8s/production` dizinlerini bekler. Manifestler hazir degilse deploy job bilincli olarak fail olur. Bu davranis yanlislikla bos deploy gecmesini engeller.