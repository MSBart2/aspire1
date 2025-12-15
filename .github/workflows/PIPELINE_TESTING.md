# Pipeline Testing Checklist

Use this checklist when testing the multistage pipeline for the first time or after making changes.

## Prerequisites ✅

Before testing, ensure you have:

- [ ] Created all three GitHub environments (dev, stage, prod)
- [ ] Configured required approvers for stage and prod
- [ ] Created Azure service principals with OIDC federation
- [ ] Added all required secrets to GitHub
- [ ] Added all required variables to GitHub (optional)
- [ ] Service principals have Contributor role on subscriptions
- [ ] Branch protection rules configured (for prod)

## Test 1: Dev Deployment (Automatic)

**Trigger:** Push to main branch

### Steps:
1. Create a test branch
   ```bash
   git checkout -b test/pipeline-dev
   ```

2. Make a minor change (e.g., update README)
   ```bash
   echo "# Test dev deployment" >> README.md
   git add README.md
   git commit -m "Test: dev deployment"
   ```

3. Push to main (or create PR and merge)
   ```bash
   git push origin test/pipeline-dev
   # Create PR, merge to main
   ```

4. Monitor workflow in GitHub Actions

### Expected Results:
- [ ] Build stage completes successfully (~2-3 min)
- [ ] Test Web completes successfully (~1-2 min)
- [ ] Test API completes successfully (~1-2 min)
- [ ] Deploy Dev completes successfully (~3-5 min)
- [ ] Deploy Stage skipped (not a tag)
- [ ] Deploy Prod skipped (not a tag)
- [ ] Health check passes
- [ ] Version endpoint returns correct version
- [ ] Deployment summary created

### Verify:
```bash
# Check dev environment endpoints
azd env select dev
azd show

# Test health endpoint
curl https://<dev-api-url>/health
curl https://<dev-api-url>/version

# Check Application Insights
# Azure Portal → Application Insights → Live Metrics
```

## Test 2: Stage Deployment (Manual Approval)

**Trigger:** Create and push version tag

### Steps:
1. Ensure main branch is stable
   ```bash
   git checkout main
   git pull
   ```

2. Create a test version tag
   ```bash
   git tag v0.0.1-test
   git push origin v0.0.1-test
   ```

3. Monitor workflow in GitHub Actions

4. When stage deployment waits for approval:
   - [ ] Go to Actions → Running workflow → Review deployments
   - [ ] Review changes and approve stage deployment
   - [ ] Monitor stage deployment progress

### Expected Results:
- [ ] Build stage completes
- [ ] Test stages complete
- [ ] Deploy Dev completes automatically
- [ ] Deploy Stage waits for approval
- [ ] After approval, Deploy Stage completes (~3-5 min)
- [ ] Deploy Prod waits for approval
- [ ] Health checks pass for both dev and stage
- [ ] Deployment summaries created

### Verify:
```bash
# Check stage environment endpoints
azd env select stage
azd show

# Test health endpoint
curl https://<stage-api-url>/health
curl https://<stage-api-url>/version

# Verify version matches tag
curl https://<stage-api-url>/version | jq '.version'
# Should show: "0.0.1-test" or similar
```

## Test 3: Prod Deployment (Manual Approval + Wait)

**Continuing from Test 2...**

### Steps:
1. After stage deployment completes, prod waits for approval
2. Wait for 5-minute cooling period (or adjust in workflow)
3. Review deployment:
   - [ ] Verify stage environment is healthy
   - [ ] Check Application Insights for errors
   - [ ] Review deployment checklist in summary
4. Approve prod deployment

### Expected Results:
- [ ] 5-minute wait timer completes
- [ ] After approval, Deploy Prod completes (~3-5 min)
- [ ] Health checks pass for prod
- [ ] Post-deployment checklist shown in summary
- [ ] All environments running same version

### Verify:
```bash
# Check prod environment endpoints
azd env select prod
azd show

# Test health endpoint
curl https://<prod-api-url>/health
curl https://<prod-api-url>/version

# Verify all environments match
curl https://<dev-api-url>/version | jq '.version'
curl https://<stage-api-url>/version | jq '.version'
curl https://<prod-api-url>/version | jq '.version'
# All should show same version
```

## Test 4: Manual Dispatch

**Trigger:** Manual workflow dispatch

### Steps:
1. Go to Actions → Multistage Build and Deploy → Run workflow
2. Configure:
   - Branch: main
   - Environment: dev
   - Skip tests: false
3. Click "Run workflow"
4. Monitor execution

### Expected Results:
- [ ] Workflow runs with selected configuration
- [ ] Only selected environment deployed
- [ ] Other environments skipped
- [ ] All stages complete successfully

## Test 5: Skip Tests (Emergency Mode)

**Trigger:** Manual workflow dispatch with skip_tests

### Steps:
1. Go to Actions → Multistage Build and Deploy → Run workflow
2. Configure:
   - Branch: main
   - Environment: dev
   - Skip tests: **true**
3. Click "Run workflow"
4. Monitor execution

### Expected Results:
- [ ] Build stage completes
- [ ] Test stages skipped
- [ ] Deploy stage(s) complete
- [ ] Total time reduced (~4-7 min vs 6-10 min)

### Warning:
Only use skip_tests for genuine emergencies. Normal workflow should include tests.

## Test 6: Parallel Execution

**Verify:** Tests run in parallel

### Steps:
1. Trigger any workflow (push to main or tag)
2. Monitor Actions tab during test phase
3. Observe timing

### Expected Results:
- [ ] Test Web and Test API start at same time
- [ ] Both complete in ~1-2 minutes each
- [ ] Deploy Dev waits for both to complete
- [ ] Total test time ~1-2 min (not 2-4 min sequential)

## Test 7: Failure Scenarios

### 7a. Test Failure

**Setup:** Temporarily break a test

### Steps:
1. Modify a test to fail
   ```bash
   # Edit aspire1.Web.Tests/UnitTest1.cs
   # Change an assertion to fail
   ```
2. Commit and push to main
3. Monitor workflow

### Expected Results:
- [ ] Build succeeds
- [ ] Test fails
- [ ] Deploy stages skipped
- [ ] Workflow fails overall
- [ ] Test results published showing failure

### Cleanup:
```bash
git revert HEAD
git push
```

### 7b. Deployment Failure

**Setup:** Temporarily misconfigure Azure

### Steps:
1. Change a secret to invalid value
2. Trigger workflow
3. Monitor execution

### Expected Results:
- [ ] Build and tests succeed
- [ ] Azure login or azd up fails
- [ ] Clear error message shown
- [ ] Workflow marked as failed
- [ ] Other environments not attempted

### Cleanup:
Fix secret and re-run workflow

## Test 8: Caching Performance

**Verify:** NuGet caching works

### Steps:
1. Run workflow twice in succession
2. Compare restore times

### Expected Results:
- [ ] First run: Restore ~30-60 sec (cache miss)
- [ ] Second run: Restore ~5-10 sec (cache hit)
- [ ] Cache key matches across runs
- [ ] Build time reduced significantly on second run

## Test 9: Artifact Reuse

**Verify:** Build artifacts used across stages

### Steps:
1. Monitor workflow with detailed logs enabled
2. Check if build artifacts uploaded/downloaded

### Expected Results:
- [ ] Build stage uploads artifacts
- [ ] Artifacts include compiled assemblies
- [ ] Artifact retention set to 1 day
- [ ] Multiple deployments can use same build

## Test 10: Security & OIDC

**Verify:** OIDC authentication works without secrets

### Steps:
1. Check workflow logs for authentication
2. Verify no client secrets in logs

### Expected Results:
- [ ] Azure login succeeds with OIDC
- [ ] No secrets or passwords in logs
- [ ] Correct subscription accessed
- [ ] Managed identity used where applicable

## Troubleshooting Common Issues

### Issue: Environment not found
**Solution:** Create environment in Settings → Environments

### Issue: OIDC authentication fails
**Solution:**
1. Verify federated credential configured correctly
2. Check subject claim matches: `repo:ORG/REPO:environment:ENV`
3. Ensure service principal has correct role

### Issue: Approval not showing
**Solution:**
1. Check environment protection rules
2. Verify required approvers configured
3. Check if approver has access to repository

### Issue: azd command fails
**Solution:**
1. Check Azure subscription ID correct
2. Verify service principal permissions
3. Check azd environment configuration
4. Review detailed error in logs

### Issue: Health check fails
**Solution:**
1. Wait longer (containers may still be starting)
2. Check Container App logs in Azure Portal
3. Verify Application Insights shows app is running
4. Check for startup errors in logs

## Final Validation

After all tests pass:

- [ ] Dev environment healthy
- [ ] Stage environment healthy
- [ ] Prod environment healthy
- [ ] All running same version
- [ ] Application Insights receiving data
- [ ] Custom metrics working
- [ ] Feature flags configured
- [ ] Alerts configured
- [ ] Dashboard accessible

## Cleanup (Optional)

If this was a test run, clean up:

```bash
# Delete test environments
azd env select dev && azd down --force --purge
azd env select stage && azd down --force --purge
azd env select prod && azd down --force --purge

# Delete test tag
git tag -d v0.0.1-test
git push origin :refs/tags/v0.0.1-test
```

## Documentation Complete ✅

Once all tests pass, pipeline is ready for production use!

Next steps:
1. Document specific environment URLs for your team
2. Set up monitoring alerts
3. Train team on approval process
4. Establish release cadence
5. Monitor pipeline performance metrics
