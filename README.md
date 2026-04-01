# NekoT Token Monitor

**v0.1.0 Early Open Source Preview**

---

## 🤖 AI-Assisted Development Declaration

This project adopts an **AI-Assisted Development** model, where the core code architecture is designed by human developers, and AI assistants help with code implementation, optimization, and documentation.

**AI-Assisted Development Tools**:
- Code Generation & Refactoring: Claude, GPT-4, and other LLMs
- Code Review & Optimization: AI-assisted Code Review
- Documentation: AI-assisted technical documentation generation

**Development Philosophy**: Human-AI collaboration, AI empowerment, improved development efficiency and code quality.

---

## 📥 Direct Download for Users in China

**👉 [Click here to visit the Releases page for the latest version](https://github.com/gaowatch/nekot/releases)**

> 💡 **Tip**: Download the exe file and double-click to run, No compilation required.

---

## 📖 Project Overview

NekoT is a Token monitoring browser designed for AI developers and researchers, supporting real-time monitoring of Token usage across multiple LLM platforms.

### ✨ Core Features

- 🔍 **Real-time Token Monitoring** - Automatically capture and track Token usage in AI conversations
- 🌐 **Multi-Platform Support** - Supports OpenAI, Claude, DeepSeek, Kimi, Zhipu, Doubao, and other major platforms
- 📊 **Usage Statistics** - Detailed Token usage records and cost estimation
- 🔒 **Secure Storage** - API Key encrypted locally to protect your privacy
- 🌍 **Internationalization** - Supports both Chinese and English interfaces

---

## ⚠️ Official Version

**Official Repository**: `https://github.com/gaowatch/nekot`

Any other software with the same or similar name is a counterfeit. Please verify the authenticity.

---

## ⚠️ Important Disclaimer and Usage Restrictions

1. **Usage Limitation**: This tool is only for **personal legal monitoring of your own LLM API usage and local API forwarding debugging**. It is prohibited for any illegal or unauthorized purposes (including but not limited to stealing others' API Keys, automated batch attacks, bypassing restrictions, or data theft).

2. **Liability**: Users must comply with local laws and regulations, as well as the API usage agreements/terms of service of the corresponding LLM providers. **The tool developers are not responsible for any user's breach of contract or illegal activities**.

3. **Data Security**: This tool runs 100% locally. It **does not collect, store, or upload any user sensitive data** (including API Keys, Tokens, conversation content, etc.). All data is processed temporarily in the user's local device memory and completely destroyed after the program exits.

4. **Functional Boundaries**: This tool only provides transparent forwarding capabilities and does not offer any features to bypass provider restrictions, batch operations, or multi-account operations.

---

## 🔒 Security Encryption

This project uses **AES-256-GCM algorithm** to encrypt sensitive data (such as Tokens, user configurations):
- AES-256 is the encryption standard recommended by the US NSA for top-secret information, with 256-bit key length ensuring extremely high security
- GCM mode provides triple protection of confidentiality, integrity, and authenticity, preventing data leakage and tampering
- Encryption implementation relies on globally audited open-source encryption libraries (such as OpenSSL), without custom encryption logic
- Technically meets CC EAL3+ level encryption security requirements (not formally certified)

---

## 🚀 Quick Start

### Option 1: Direct Run (Recommended for Beginners)

1. Visit the [Releases](https://github.com/gaowatch/nekot/releases) page
2. Download the latest version of `NekoT.Desktop.exe` or the compressed package
3. Double-click to run, No installation required

### Option 2: Build from Source

```bash
# Clone the repository
git clone https://github.com/gaowatch/nekot.git
cd nekot

# Restore dependencies
dotnet restore

# Build and run
dotnet run --project NekoT.Desktop
```

### System Requirements

- Windows 10/11 (x64)
- .NET 8.0 Runtime
- WebView2 Runtime (usually pre-installed on Windows 10/11)

---

## 📜 License

This project is open-sourced under the [Apache License 2.0](LICENSE).

---

## 🤝 Contributing

Issues and Pull Requests are welcome!

---

## 📞 Contact

For questions or suggestions, please submit feedback via [GitHub Issues](https://github.com/gaowatch/nekot/issues).

---

**NekoT** - Making Token Monitoring Simple 🔐
