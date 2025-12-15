# CI/CD Pipeline Documentation Index

Welcome to the aspire1 CI/CD pipeline documentation! This index helps you find the right documentation for your needs.

## 📚 Documentation Overview

| Document | Purpose | When to Read |
|----------|---------|-------------|
| **[PIPELINE_COMPARISON.md](PIPELINE_COMPARISON.md)** | Compare simple vs multistage pipelines | Deciding which pipeline to use |
| **[PIPELINE_QUICK_REFERENCE.md](PIPELINE_QUICK_REFERENCE.md)** | Quick usage guide and common workflows | Day-to-day development |
| **[PIPELINE_SETUP.md](PIPELINE_SETUP.md)** | Complete setup instructions | Initial configuration |
| **[PIPELINE_DIAGRAMS.md](PIPELINE_DIAGRAMS.md)** | Visual architecture diagrams | Understanding pipeline flow |
| **[PIPELINE_TESTING.md](PIPELINE_TESTING.md)** | Testing checklist | First-time setup or changes |

## 🚀 Quick Start Guide

### I'm a Developer - What Do I Need?

**For daily work:**
1. Read [PIPELINE_QUICK_REFERENCE.md](PIPELINE_QUICK_REFERENCE.md)
2. Learn common workflows (push to main, create tags, manual dispatch)
3. Understand when to use simple vs multistage pipeline

**For releases:**
1. Understand the [approval process](PIPELINE_QUICK_REFERENCE.md#approval-process)
2. Know how to [create version tags](PIPELINE_QUICK_REFERENCE.md#2-release-to-production)
3. Monitor deployment progress in GitHub Actions

### I'm Setting Up the Pipeline - What Do I Need?

**Initial setup:**
1. Follow [PIPELINE_SETUP.md](PIPELINE_SETUP.md) step by step
2. Configure Azure service principals with OIDC
3. Create GitHub environments and secrets
4. Run through [PIPELINE_TESTING.md](PIPELINE_TESTING.md) checklist

**Understanding the architecture:**
1. Review [PIPELINE_DIAGRAMS.md](PIPELINE_DIAGRAMS.md) for visual overview
2. Understand stage dependencies and parallel execution
3. Learn security and authentication flow

### I Need to Decide Which Pipeline to Use - What Do I Need?

Read [PIPELINE_COMPARISON.md](PIPELINE_COMPARISON.md) to understand:
- Feature comparison between simple and multistage
- When to use each pipeline
- Cost considerations
- Migration path

## 📖 Documentation Roadmap

### Phase 1: Understanding (30 minutes)
1. **[PIPELINE_COMPARISON.md](PIPELINE_COMPARISON.md)** - Understand the two pipelines (5 min)
2. **[PIPELINE_DIAGRAMS.md](PIPELINE_DIAGRAMS.md)** - Visual architecture (10 min)
3. **[PIPELINE_QUICK_REFERENCE.md](PIPELINE_QUICK_REFERENCE.md)** - Common workflows (15 min)

### Phase 2: Setup (2-4 hours)
1. **[PIPELINE_SETUP.md](PIPELINE_SETUP.md)** - Follow all setup steps (2-3 hours)
2. **[PIPELINE_TESTING.md](PIPELINE_TESTING.md)** - Validate everything works (1 hour)

### Phase 3: Daily Use (ongoing)
1. **[PIPELINE_QUICK_REFERENCE.md](PIPELINE_QUICK_REFERENCE.md)** - Reference for workflows
2. GitHub Actions UI - Monitor deployments
3. Azure Portal - Verify resources and monitoring

## 🎯 Common Scenarios

### "I want to deploy my changes quickly"
→ See [PIPELINE_QUICK_REFERENCE.md § Daily Development](PIPELINE_QUICK_REFERENCE.md#1-daily-development-use-multistage-for-safety)

### "I need to release to production"
→ See [PIPELINE_QUICK_REFERENCE.md § Release to Production](PIPELINE_QUICK_REFERENCE.md#2-release-to-production)

### "I have a critical hotfix"
→ See [PIPELINE_QUICK_REFERENCE.md § Hotfix to Production](PIPELINE_QUICK_REFERENCE.md#3-hotfix-to-production)

### "Setup is not working"
→ See [PIPELINE_SETUP.md § Troubleshooting](PIPELINE_SETUP.md#troubleshooting)

### "I need to understand the pipeline architecture"
→ See [PIPELINE_DIAGRAMS.md](PIPELINE_DIAGRAMS.md)

### "Which pipeline should I use?"
→ See [PIPELINE_COMPARISON.md § Quick Decision Matrix](PIPELINE_COMPARISON.md#quick-decision-matrix)

### "I'm testing the pipeline for the first time"
→ See [PIPELINE_TESTING.md](PIPELINE_TESTING.md)

## 🔑 Key Concepts

### Workflows

| Workflow | File | Purpose |
|----------|------|---------|
| **Multistage Deploy** | `multistage-deploy.yml` | Production pipeline with 3 environments |
| **Simple Deploy** | `deploy.yml` | Quick single-environment deployment |

### Environments

| Environment | Purpose | Approval | Deployment |
|-------------|---------|----------|------------|
| **Dev** | Development/testing | None | Automatic on push to main |
| **Stage** | Pre-production | 1-2 reviewers | Manual approval after dev |
| **Prod** | Production | 2+ reviewers + 5min wait | Manual approval after stage |

### Pipeline Stages

1. **Build & Version** - Compile code, extract version with MinVer
2. **Test (Parallel)** - Run Web.Tests and WeatherService.Tests simultaneously
3. **Deploy Dev** - Deploy to development environment
4. **Deploy Stage** - Deploy to staging (requires approval)
5. **Deploy Prod** - Deploy to production (requires approval + wait)

## 🛠️ Tools & Technologies

- **GitHub Actions** - CI/CD automation platform
- **Azure Developer CLI (azd)** - Infrastructure provisioning and deployment
- **Azure Container Apps** - Hosting platform
- **OIDC** - Secure authentication without secrets
- **MinVer** - Automatic semantic versioning
- **.NET 9** - Application framework

## 📊 Pipeline Metrics

Track these metrics for pipeline health:

| Metric | Target | Where to Check |
|--------|--------|----------------|
| **Build Success Rate** | >95% | GitHub Actions insights |
| **Test Success Rate** | >98% | GitHub Actions insights |
| **Deployment Time (Dev)** | <10 min | Workflow run summary |
| **Deployment Time (Full)** | <20 min | Workflow run summary |
| **Pipeline Failure Rate** | <5% | GitHub Actions insights |

## 🔐 Security

The pipeline uses **OIDC authentication** for security:

- ✅ No secrets stored in GitHub (only client IDs)
- ✅ Time-limited tokens
- ✅ Environment-specific service principals
- ✅ Least-privilege access (Contributor role per subscription)
- ✅ Audit trail in Azure AD

Learn more in [PIPELINE_SETUP.md § Security Best Practices](PIPELINE_SETUP.md#security-best-practices)

## 📞 Getting Help

### Documentation Issues
- **Missing information?** Check other documents in this directory
- **Need clarification?** See troubleshooting sections in each guide
- **Found a bug?** Review [PIPELINE_TESTING.md](PIPELINE_TESTING.md) for validation steps

### Azure Issues
- **Resource provisioning errors?** Check service principal permissions
- **Deployment failures?** Review Container App logs in Azure Portal
- **Application not working?** Check Application Insights for errors

### GitHub Actions Issues
- **Workflow not triggering?** Verify branch names and trigger conditions
- **Approval not showing?** Check environment protection rules
- **OIDC auth failing?** Validate federated credentials

## 🎓 Learning Path

### Beginner (New to the Project)
1. Start with [PIPELINE_COMPARISON.md](PIPELINE_COMPARISON.md)
2. Review [PIPELINE_QUICK_REFERENCE.md](PIPELINE_QUICK_REFERENCE.md)
3. Try pushing to main and watch the pipeline run
4. Understand the dev deployment workflow

### Intermediate (Need to Setup Environments)
1. Complete all steps in [PIPELINE_SETUP.md](PIPELINE_SETUP.md)
2. Work through [PIPELINE_TESTING.md](PIPELINE_TESTING.md)
3. Study [PIPELINE_DIAGRAMS.md](PIPELINE_DIAGRAMS.md) for architecture
4. Practice manual dispatch and approval workflows

### Advanced (Optimizing and Customizing)
1. Understand parallel execution in [PIPELINE_DIAGRAMS.md](PIPELINE_DIAGRAMS.md)
2. Learn caching strategies for performance
3. Customize environment-specific configurations
4. Set up monitoring and alerting

## 📈 Pipeline Evolution

The pipeline has been designed for evolution:

**Current State:**
- 3 environments (dev, stage, prod)
- Parallel testing
- Manual approvals for stage/prod
- OIDC authentication

**Future Enhancements (Ideas):**
- Integration tests in pipeline
- Performance testing stage
- Blue/green deployments
- Canary releases
- Automated rollback
- Deployment notifications (Slack, Teams)

## 🤝 Contributing

When modifying the pipeline:

1. Test changes in a feature branch
2. Validate workflow syntax with yamllint
3. Update relevant documentation
4. Run through [PIPELINE_TESTING.md](PIPELINE_TESTING.md) checklist
5. Document breaking changes clearly

## 📚 Additional Resources

### Project Documentation
- [Main README.md](../../README.md) - Project overview
- [ARCHITECTURE.md](../../ARCHITECTURE.md) - Solution architecture
- [TELEMETRY.md](../../TELEMETRY.md) - Observability details

### External Resources
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Azure Developer CLI Documentation](https://learn.microsoft.com/azure/developer/azure-developer-cli/)
- [Azure Container Apps Documentation](https://learn.microsoft.com/azure/container-apps/)
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [OIDC with Azure](https://docs.github.com/en/actions/deployment/security-hardening-your-deployments/configuring-openid-connect-in-azure)

## 🎉 Success Criteria

You'll know the pipeline is working when:

- ✅ Push to main automatically deploys to dev in <10 minutes
- ✅ Tags trigger full pipeline (dev → stage → prod)
- ✅ Tests run in parallel and complete in <2 minutes each
- ✅ Approvals work correctly for stage and prod
- ✅ Health checks pass after each deployment
- ✅ Application Insights receives telemetry
- ✅ All environments show same version after release
- ✅ Team understands how to use both pipelines

---

**Questions?** Start with the document that matches your current need, and follow the links within for deeper information.

**Ready to begin?** Jump to [PIPELINE_QUICK_REFERENCE.md](PIPELINE_QUICK_REFERENCE.md) for common workflows!
