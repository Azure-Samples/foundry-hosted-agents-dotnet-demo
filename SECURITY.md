# Security

Microsoft takes the security of our software products and services seriously. This includes all source code repositories managed through our GitHub organizations.

## Reporting Security Vulnerabilities

**Please do not report security vulnerabilities through public GitHub issues.**

Instead, please report them to the Microsoft Security Response Center (MSRC) at [https://msrc.microsoft.com/create-report](https://msrc.microsoft.com/create-report).

If you prefer to submit without logging in, send email to [secure@microsoft.com](mailto:secure@microsoft.com). If possible, encrypt your message with our PGP key — download it from the [Microsoft Security Response Center PGP Key page](https://www.microsoft.com/msrc/pgp-key-msrc).

You should receive a response within 24 hours. If you do not, please follow up via email to ensure we received your original message.

## Security Best Practices in This Lab

This learning lab demonstrates secure patterns for hosted agents:

- **No secrets in containers** — Use managed identities, not API keys in environment variables
- **Credential strategy** — Local dev uses `AzureCliCredential` (from `az login`); containers use `DefaultAzureCredential` (managed identity)
- **Azure Key Vault** — For production secrets beyond auth, use [Azure Key Vault](https://learn.microsoft.com/en-us/azure/key-vault/)

See the [Security section](README.md#security) in the main README for details.

## Additional Resources

- [Microsoft Security Response Center](https://www.microsoft.com/msrc/)
- [Microsoft Bug Bounty Program](https://microsoft.com/msrc/bounty)
- [Coordinated Vulnerability Disclosure](https://www.microsoft.com/msrc/cvd)
