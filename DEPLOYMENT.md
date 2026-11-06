# Automatic deployment to SmarterASP

## Setup once

This repository's workflow verifies pull requests and deploys pushes to `main`.
You can also use Actions > Verify and deploy backend > Run workflow
with the `main` branch. A manual run on another branch only builds.

In SmarterASP, open Websites > **site1** > **VSDeploy**.
Download the publishing settings for this specific site. Treat this file as a password:
do not commit it or send its contents in a screenshot.

In this GitHub repository, open Settings > Secrets and variables > Actions.
Create these four **repository secrets**, taking the values from that site's
Web Deploy publishing profile (not the FTP profile):

| Secret | Value |
| --- | --- |
| `SMARTERASP_SITE_NAME` | `msdeploySite`; expected: `h2coaching-001-site1` |
| `SMARTERASP_SERVER_URL` | HTTPS Web Deploy endpoint; e.g. `https://WIN_SERVER.site4now.net:8172/msdeploy.axd?site=h2coaching-001-site1` |
| `SMARTERASP_USERNAME` | `userName` from the Web Deploy profile |
| `SMARTERASP_PASSWORD` | `userPWD` from the Web Deploy profile |

Use the actual server from VSDeploy. If `publishUrl` contains only a hostname
and port, build the full HTTPS endpoint shown above using that hostname and
the exact `msdeploySite`. Do not use the public website domain as this endpoint.
If the profile does not include a password, set/enable Web Deploy credentials
in the hosting panel and enter the password directly in GitHub Secrets.

Keep the same secret names in both repositories, but take their values from
the correct site profile. The script rejects the other site's name.

## Behavior

- Restores, builds and publishes only `H2-Trainning/H2-Trainning.csproj` with .NET 10.
- Changes to the sample TestApp projects do not deploy the API.
- Does not run database migrations or database integration tests. Schema changes
  require a separate reviewed migration step against the intended database.
- Preserves the live `appsettings*.json` and `web.config`, including database/JWT
  configuration and IIS environment settings. Server configuration changes
  must be applied separately; this pipeline is for the existing configured site.
- Uses Web Deploy's AppOffline rule while updating DLLs. Expect a brief restart.
- Preserves uploads, App_Data, logs and SSL validation files.
- Checks the public Swagger document after deployment.

Deployment uses Microsoft Web Deploy over HTTPS. If absent on the Windows
runner, the script installs it using Chocolatey. The script validates the
server certificate; fix any endpoint/certificate issue rather than disabling TLS.
It does not delete destination files, so existing server files remain.
Production runs are serialized and are not interrupted by a newer push.

## First run

1. Add all four secrets before pushing the workflow and application changes.
2. Push to `main` and open Actions.
3. Confirm the build, deployment and public checks are green.
4. Test login and application data in the browser. Public checks do not log in.

If deployment fails, review the failing Actions step. Missing secrets and a
wrong site name fail before connecting. A connection/authorization error usually
means Web Deploy is disabled or the endpoint/credentials are incorrect.
If an interrupted deployment leaves `app_offline.htm` in the API root, inspect the failure and remove that deployment file through File Manager to bring the application online.

## Reference

[SmarterASP's GitHub Actions deployment guide](https://www.smarterasp.net/support/kb/a2275/how-to-use-github-actions-to-auto-publish-your-website-file-to-our-server_.aspx)
