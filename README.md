# NekoT Token Monitor

**v0.1.0 早期开源预览版**

---

## 📥 国内用户直接下载入口

**👉 [点击前往 Releases 页面下载最新版本](https://github.com/gaowatch/nekot/releases)**

> 💡 **提示**：下载 exe 双击即可使用，无需编译。

---

## 📖 项目简介

NekoT 是一款专为 AI 开发者和研究者设计的 Token 监控浏览器，支持实时监控多个 LLM 平台的 Token 使用情况。

### ✨ 核心功能

- 🔍 **实时 Token 监控** - 自动捕获和统计 AI 对话中的 Token 使用量
- 🌐 **多平台支持** - 支持 OpenAI、Claude、DeepSeek、Kimi、智谱、豆包等主流平台
- 📊 **使用统计** - 详细的 Token 使用记录和费用估算
- 🔒 **安全存储** - API Key 本地加密存储，保护您的隐私
- 🌍 **国际化** - 支持中文和英文界面

---

## ⚠️ 唯一原版

**官方仓库**：`https://github.com/gaowatch/nekot`

任何其他同名/相似软件均为仿品，请注意甄别。

---

## ⚠️ 重要声明与使用限制

1. **用途限制**：本工具仅用于**个人合法监控自身 LLM API 用量、本地 API 转发调试**，禁止用于任何违法、违规用途（包括但不限于盗刷他人 API Key、批量自动化攻击、翻墙、窃密等）。
2. **责任划分**：使用者需自行遵守所在国家/地区的法律法规，以及对应大模型厂商的 API 使用协议/用户服务协议。**工具开发者不对任何使用者的违约、违法行为承担任何责任**。
3. **数据安全**：本工具 100% 本地运行，**不收集、不存储、不上传任何用户敏感数据**（包括 API Key、Token、对话内容等），所有数据仅在用户本地设备内存中临时处理，程序退出后彻底销毁。
4. **功能边界**：本工具仅提供纯透传转发能力，不提供任何绕过厂商风控、批量调用、多账号操作等违规功能。

---

## 🔒 安全加密

本项目采用 **AES-256-GCM 算法** 对敏感数据（如 Token、用户配置）进行加密保护：
- AES-256 是美国 NSA 推荐用于绝密信息的加密标准，256 位密钥长度确保极高安全性
- GCM 模式提供机密性、完整性和真实性三重保障，防止数据泄露与篡改
- 加密实现依赖经过全球审计的开源加密库（如 OpenSSL），未使用自定义加密逻辑
- 技术层面达到 CC EAL3+ 级别的加密安全要求（未进行正式认证）

---

## 🚀 快速开始

### 方式一：直接运行（推荐小白用户）

1. 前往 [Releases](https://github.com/gaowatch/nekot/releases) 页面
2. 下载最新版本的 `NekoT.Desktop.exe` 或压缩包
3. 双击运行即可，无需安装

### 方式二：从源码编译

```bash
# 克隆仓库
git clone https://github.com/gaowatch/nekot.git
cd nekot

# 还原依赖
dotnet restore

# 编译运行
dotnet run --project NekoT.Desktop
```

### 系统要求

- Windows 10/11 (x64)
- .NET 8.0 Runtime
- WebView2 Runtime（Windows 10/11 通常已预装）

---

## 📜 许可证

本项目采用 [Apache License 2.0](LICENSE) 许可证开源。

---

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

---

## 📞 联系方式

如有问题或建议，请通过 [GitHub Issues](https://github.com/gaowatch/nekot/issues) 反馈。

---

**NekoT** - 让 Token 监控变得简单 🔐
