# Pipeline Quick Reference

## Which Pipeline Runs When?

### Multistage Pipeline (`multistage-deploy.yml`)

**Recommended for:** Production deployments, releases, multi-environment workflows

| Scenario | Trigger | Environments Deployed | Time |
|----------|---------|----------------------|------|
| Push to `main` | Automatic | Dev only | ~6-10 min |
| Create tag `v*` | Automatic | Dev → Stage (approval) → Prod (approval) | ~15-20 min |
| Manual dispatch | Click "Run workflow" | Selected environment | ~6-10 min per env |

**Features:**
- ✅ Parallel testing (Web + API)
- ✅ Manual approvals for stage/prod
- ✅ Environment-specific configurations
- ✅ Health checks and verification
- ✅ OIDC authentication (no secrets)

### Simple Pipeline (`deploy.yml`)

**Recommended for:** Quick iterations, testing, single environment

| Scenario | Trigger | Environments Deployed | Time |
|----------|---------|----------------------|------|
| Push to `main` | Automatic | Dev only | ~3-5 min |
| Create tag `v*` | Automatic | Dev only | ~3-5 min |
| Manual dispatch | Click "Run workflow" | Selected environment | ~3-5 min |

**Features:**
- ✅ Fast deployment
- ✅ Single environment
- ✅ No approvals
- ✅ Simple workflow

## Common Workflows

### 1. Daily Development (Use Multistage for Safety)

```bash
# Work on feature branch
git checkout -b feature/my-feature
# ... make changes ...
git add .
git commit -m "Add my feature"
git push origin feature/my-feature

# Create PR, merge to main
# → Multistage pipeline automatically deploys to dev
```

**Result:** Deployed to dev in ~6-10 minutes after tests pass

### 2. Release to Production

```bash
# Ensure main is stable
git checkout main
git pull

# Create release tag
git tag -a v1.2.3 -m "Release v1.2.3: Added X feature"
git push origin v1.2.3
```

**Result:** 
1. Deploys to dev automatically (~6-10 min)
2. Waits for approval to deploy to stage
3. After stage approval, waits for approval to deploy to prod
4. Total time: ~15-20 minutes including approvals

### 3. Hotfix to Production

**Option A: Full pipeline (recommended)**
```bash
# Create hotfix branch
git checkout -b hotfix/critical-fix main
# ... fix issue ...
git commit -m "Fix critical bug"

# Create hotfix tag
git tag v1.2.4
git push origin v1.2.4
```

**Option B: Emergency deployment (skip tests)**
1. Go to **Actions** → **Multistage Build and Deploy**
2. Click **Run workflow**
3. Select:
   - Branch: `main` or hotfix branch
   - Environment: `prod`
   - Skip tests: `true` (only if emergency!)
4. Approve through environments

### 4. Testing in Staging Only

1. Go to **Actions** → **Multistage Build and Deploy**
2. Click **Run workflow**
3. Select:
   - Branch: `main` or feature branch
   - Environment: `stage`
   - Skip tests: `false`
4. Approve deployment

### 5. Quick Dev Iteration (Use Simple Pipeline)

```bash
# For rapid testing without approvals
git checkout -b test/quick-fix
# ... make changes ...
git commit -m "Test fix"
git push

# Trigger simple pipeline via workflow dispatch
# Select environment: dev
```

## Pipeline Selection Guide

| Need | Use This Pipeline | Why |
|------|------------------|-----|
| **Production release** | Multistage | Multiple environments, approvals, safety |
| **Pre-prod testing** | Multistage | Stage environment with approval |
| **Daily dev work** | Multistage | Automatic dev deployment with tests |
| **Quick experiment** | Simple | Fast, no approvals, single environment |
| **Demo environment** | Simple | Quick setup for demos |
| **Breaking changes** | Multistage | Test in dev/stage before prod |

## Approval Process

### Stage Deployment
- **Required approvers:** 1-2 people
- **Wait time:** 0 minutes
- **Can skip:** No (unless using simple pipeline)

### Prod Deployment
- **Required approvers:** 2+ people
- **Wait time:** 5 minutes (cooling period)
- **Can skip:** No (unless using simple pipeline)
- **Branch restrictions:** Only protected branches

## Environment URLs

After deployment, find your environment URLs:

```bash
# For dev
azd env select dev
azd show

# For stage
azd env select stage
azd show

# For prod
azd env select prod
azd show
```

Or check the workflow run summary in GitHub Actions.

## Troubleshooting

### Pipeline not triggering?
- Check branch name (must be `main` for automatic triggers)
- Verify tag format (must start with `v`, e.g., `v1.0.0`)
- Check if workflows are enabled in repository settings

### Stuck waiting for approval?
- Check **Environments** in repository settings
- Verify required reviewers are available
- Admin can override protection rules if needed

### Tests failing?
- Check test logs in GitHub Actions
- Run tests locally: `dotnet test`
- Use workflow dispatch with `skip_tests: true` for emergency (not recommended)

### Deployment failed?
- Check Azure login succeeded (OIDC configuration)
- Verify service principal has correct permissions
- Check azd environment configuration
- View detailed logs in GitHub Actions

## Best Practices

1. **Use multistage for production** - Always use approvals for prod
2. **Test in dev first** - Never skip dev deployment
3. **Tag releases** - Use semantic versioning (v1.2.3)
4. **Don't skip tests** - Only for genuine emergencies
5. **Review before approving** - Check dev/stage before approving prod
6. **Monitor after deployment** - Check Application Insights for errors
7. **Use feature flags** - Toggle features without redeploying

## Monitoring Deployments

### During Deployment
- Watch the workflow run in GitHub Actions
- Each step shows real-time logs
- Summary appears after deployment completes

### After Deployment
- Check service health: `curl <url>/health`
- Check version: `curl <url>/version`
- Monitor Application Insights for errors
- Review custom metrics dashboard
- Verify feature flags in Azure App Configuration

## Getting Help

- **Setup issues:** See `.github/workflows/PIPELINE_SETUP.md`
- **Architecture:** See `ARCHITECTURE.md`
- **Visual guide:** See `.github/workflows/PIPELINE_DIAGRAMS.md`
- **Azure resources:** See `README.md`
