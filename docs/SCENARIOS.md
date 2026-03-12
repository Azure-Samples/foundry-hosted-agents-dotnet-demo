# Hosted Agent Scenarios

> **Context**: The current demo uses a timezone agent with a single function tool. These three scenarios show more practical, compelling uses of hosted agents — things developers would actually deploy. Each follows the same `Program.cs` + `ChatClientAgent` + hosting adapter pattern.
>
> **Model**: All scenarios use `gpt-5-mini` (cost-effective, capable enough for tool orchestration).
>
> **Key principle**: Every scenario does something a prompt-only agent *cannot* do. The C# function tools perform real server-side computation.

---

## Scenario 1: Code Metrics Agent

**One-line description**: Paste code into the chat and get instant complexity analysis, line counts, naming convention checks, and anti-pattern detection.

**Why it's useful**: Code review is one of the most time-consuming parts of development. A hosted agent can run static analysis on submitted code *server-side* — no local tools needed, no IDE plugins, no CI pipeline. Developers paste a function, class, or file and get actionable feedback in seconds. This is something a prompt-only agent can approximate but gets wrong because it *guesses* at metrics instead of *computing* them.

**Function tools**:

```csharp
[Description("Counts lines of code, blank lines, and comment lines in a code snippet")]
static string AnalyzeLineMetrics(
    [Description("The source code to analyze")] string code)

[Description("Calculates cyclomatic complexity of a C# method by counting decision points")]
static string MeasureComplexity(
    [Description("The source code of a single method")] string methodCode)

[Description("Checks C# naming conventions: PascalCase for public members, camelCase for locals/params, I-prefix for interfaces")]
static string CheckNamingConventions(
    [Description("The source code to check")] string code)
```

**Example conversation**:

> **User**: Can you review this method?
> ```csharp
> public int calc(List<int> d, int t)
> {
>     int r = 0;
>     for (int i = 0; i < d.Count; i++)
>     {
>         if (d[i] > t)
>         {
>             if (d[i] % 2 == 0)
>                 r += d[i];
>             else
>                 r += d[i] * 2;
>         }
>     }
>     return r;
> }
> ```
>
> **Agent**: I ran three checks on your code. Here's what I found:
>
> 📏 **Line Metrics**: 14 lines of code, 0 comments, 1 blank line. Consider adding a summary comment.
>
> 🔀 **Cyclomatic Complexity**: **4** (moderate). You have 3 decision points (`if`, `if`, `for`). Anything above 5 deserves refactoring attention.
>
> 📛 **Naming Issues**:
> - `calc` → should be PascalCase (`Calculate` or `CalculateWeightedSum`)
> - `d` → non-descriptive parameter name (try `values` or `numbers`)
> - `t` → non-descriptive parameter name (try `threshold`)
> - `r` → non-descriptive local (try `result` or `sum`)
>
> Overall: the logic is sound but readability is poor. Want me to suggest a renamed version?

**Complexity**: **Low** — All tools are string parsing and regex. No AST parsing needed for a demo-quality implementation. ~60 lines of tool code beyond the template.

**External dependencies**: **None** — Pure C# string/regex processing. No APIs, no NuGet packages beyond the template.

---

## Scenario 2: Data Crunch Agent

**One-line description**: Feed it JSON or CSV data and get instant statistical analysis — mean, median, percentiles, outlier detection, and formatted summary tables.

**Why it's useful**: Every developer eventually needs to make sense of a dataset — API response times, sales figures, sensor readings, test results. A hosted agent can *compute real statistics* server-side instead of hallucinating them. Paste data, get facts. This is the canonical example of why server-side tools matter: LLMs are notoriously bad at math, but C# is exact.

**Function tools**:

```csharp
[Description("Parses CSV or JSON array data and returns it as a normalized table with column names and row count")]
static string ParseData(
    [Description("Raw CSV or JSON array data")] string rawData)

[Description("Computes descriptive statistics for a numeric column: count, min, max, mean, median, std dev, p25, p75")]
static string ComputeStatistics(
    [Description("Comma-separated numeric values")] string values,
    [Description("Name of the column being analyzed")] string columnName)

[Description("Detects statistical outliers using the IQR method and returns any values outside 1.5x the interquartile range")]
static string DetectOutliers(
    [Description("Comma-separated numeric values")] string values,
    [Description("Name of the column being analyzed")] string columnName)
```

**Example conversation**:

> **User**: Here's our API response time data from this morning. Anything weird?
> ```
> endpoint,response_ms,status
> /api/users,145,200
> /api/users,132,200
> /api/orders,289,200
> /api/orders,1847,500
> /api/products,98,200
> /api/products,105,200
> /api/users,138,200
> /api/orders,312,200
> /api/products,2103,500
> ```
>
> **Agent**: I parsed your CSV (9 rows, 3 columns) and crunched the numbers on `response_ms`:
>
> 📊 **Statistics**:
> | Metric | Value |
> |--------|-------|
> | Count | 9 |
> | Min | 98 ms |
> | Max | 2,103 ms |
> | Mean | 574 ms |
> | Median | 145 ms |
> | Std Dev | 731 ms |
> | P25 | 132 ms |
> | P75 | 312 ms |
>
> 🚨 **Outliers detected** (IQR method): **1,847 ms** and **2,103 ms** are well above the upper fence (582 ms). Both correlate with HTTP 500 status codes — looks like your `/api/orders` and `/api/products` endpoints had errors that spiked response times.
>
> **User**: What's the median response time for just the 200 status calls?
>
> **Agent**: Filtering to status 200 only (7 rows) and recomputing: **median response time is 138 ms**. Your healthy responses are fast — the 500s are dragging your mean up significantly.

**Complexity**: **Low–Medium** — `ParseData` needs basic CSV/JSON parsing (~30 lines). `ComputeStatistics` is straightforward math (~40 lines). `DetectOutliers` is IQR calculation (~20 lines). Total ~90 lines of tool code.

**External dependencies**: **None** — Pure C# math and string parsing. `System.Text.Json` is already in the framework.

---

## Scenario 3: Secret Scanner Agent

**One-line description**: Paste configuration files, environment variables, or code snippets and get instant detection of leaked secrets, hardcoded credentials, and security misconfigurations.

**Why it's useful**: Accidental secret leaks are one of the most common and costly security mistakes in software development. GitHub reports millions of secrets leaked in public repos every year. A hosted agent can scan text *server-side* using real regex patterns — the same approach used by tools like `gitleaks` and `trufflehog` — and flag issues before they hit a commit. This is inherently a server-side task: the scanning runs on trusted infrastructure, never exposing the secrets to a third party.

**Function tools**:

```csharp
[Description("Scans text for common secret patterns: API keys, connection strings, passwords, tokens, private keys")]
static string ScanForSecrets(
    [Description("The text content to scan for secrets")] string content)

[Description("Validates a .env or config file for security best practices: no default passwords, no localhost in production URLs, sensitive keys present")]
static string ValidateConfig(
    [Description("The configuration file content")] string configContent,
    [Description("The file name (e.g. .env, appsettings.json, docker-compose.yml)")] string fileName)

[Description("Generates a redacted version of the input with all detected secrets masked")]
static string RedactSecrets(
    [Description("The text content to redact")] string content)
```

**Example conversation**:

> **User**: I'm about to commit this `.env` file. Can you check it?
> ```
> DATABASE_URL=postgresql://admin:P@ssw0rd123@db.example.com:5432/myapp
> REDIS_URL=redis://localhost:6379
> API_KEY=sk-proj-abc123def456ghi789jkl012mno345pqr678stu901vwx
> JWT_SECRET=supersecret
> AZURE_STORAGE_CONNECTION=DefaultEndpointsProtocol=https;AccountName=myaccount;AccountKey=abcdef1234567890==;EndpointSuffix=core.windows.net
> DEBUG=true
> PORT=3000
> ```
>
> **Agent**: 🚨 **4 issues found** after scanning your `.env` file:
>
> | # | Severity | Line | Issue |
> |---|----------|------|-------|
> | 1 | 🔴 Critical | `DATABASE_URL` | Hardcoded database password in connection string (`P@ssw0rd123`) |
> | 2 | 🔴 Critical | `API_KEY` | OpenAI API key detected (`sk-proj-...`) |
> | 3 | 🟡 Warning | `JWT_SECRET` | Weak JWT secret — `supersecret` has low entropy and is a common default |
> | 4 | 🟡 Warning | `AZURE_STORAGE_CONNECTION` | Azure Storage account key embedded in connection string |
>
> **Recommendations**:
> - Use Azure Key Vault or a secrets manager for all credentials
> - Add `.env` to your `.gitignore` (verify it's not already tracked)
> - Rotate `API_KEY` and `DATABASE_URL` password — treat them as compromised if they've been in any commit
>
> Want me to generate a redacted version safe for documentation?
>
> **User**: Yes, redact it.
>
> **Agent**: Here's the redacted version:
> ```
> DATABASE_URL=postgresql://admin:****@db.example.com:5432/myapp
> REDIS_URL=redis://localhost:6379
> API_KEY=sk-proj-****
> JWT_SECRET=****
> AZURE_STORAGE_CONNECTION=DefaultEndpointsProtocol=https;AccountName=myaccount;AccountKey=****;EndpointSuffix=core.windows.net
> DEBUG=true
> PORT=3000
> ```

**Complexity**: **Low–Medium** — `ScanForSecrets` is a collection of regex patterns (~50 lines). `ValidateConfig` adds some heuristic checks (~30 lines). `RedactSecrets` reuses the scan patterns to mask matches (~20 lines). Total ~100 lines of tool code.

**External dependencies**: **None** — Pure C# regex. No external APIs, no databases, no secret-scanning services. Patterns are hardcoded (common formats for AWS keys, Azure keys, OpenAI keys, JWTs, connection strings, etc.).

---

## 🎮 GPU-Powered Scenarios

> **Context**: These scenarios leverage Azure Container Apps serverless GPUs (NVIDIA A100/T4) for compute-intensive operations. The GPU runs the function tools — not the LLM (still gpt-5-mini for orchestration). All use [ElBruno.Text2Image](https://www.nuget.org/packages/ElBruno.Text2Image) for image generation via ONNX Runtime or FLUX.2 cloud.
>
> **Key principle**: The GPU accelerates server-side computation that would be too slow on CPU (100-200× speedup). The agent pattern is identical — `Program.cs` + `ChatClientAgent` + hosting adapter — but the container targets a GPU workload profile.
>
> **Deployment**: Azure Container Apps with serverless GPU workload profile. Scale-to-zero with per-second billing. Microsoft Foundry coordinates the GPU-backed containers.

---

## Scenario 4: Image Generator Agent

**One-line description**: Chat with an agent that generates images on-demand using Stable Diffusion or FLUX.2 on GPU containers.

**Why it's useful**: Image generation is a classic GPU workload — diffusion models perform thousands of matrix operations that are 100-200× faster on GPU hardware. A text-to-image generation completes in under 2 seconds on a GPU, while CPU rendering would timeout (30+ seconds). This is a perfect entry point for showing GPU-backed hosted agents: the conversation flow is simple (user describes, agent generates), but the computational reality requires server-side GPU acceleration.

**Why GPU**: Image generation with diffusion models requires massive matrix operations — 100-200× slower on CPU. A text→image in <2 seconds is only feasible on GPU. CPU would timeout (30+ seconds).

**Function tools**:

```csharp
[Description("Generates an image from a text description using Stable Diffusion 1.5 on GPU. Returns the file path of the generated image.")]
static async Task<string> GenerateImage(
    [Description("Text description of the image to generate")] string prompt,
    [Description("Optional seed (1-999999) for reproducible results")] int? seed = null)

[Description("Generates a high-quality image using FLUX.2 via Microsoft Foundry cloud endpoint")]
static async Task<string> GenerateImageFlux(
    [Description("Text description of the image to generate")] string prompt)

[Description("Lists available image generation models and their capabilities")]
static string ListModels()
```

**Example conversation**:

> **User**: Generate a sunset over mountains.
>
> **Agent**: I'll create that for you using Stable Diffusion 1.5 on GPU...
>
> *(1.8 seconds later)*
>
> ✅ Done! Generated: `output/sunset_mountains_20260312_143022.png` (512×512, seed: 847392)
>
> The image shows a vibrant sunset with warm orange and pink tones over a mountain range silhouette. Want me to try a higher quality version with FLUX.2?
>
> **User**: Yes, use FLUX.2.
>
> **Agent**: Switching to FLUX.2 cloud endpoint for higher quality...
>
> *(2.3 seconds later)*
>
> ✅ Done! Generated: `output/sunset_mountains_flux_20260312_143028.png` (1024×1024)
>
> The FLUX.2 version has more detail in the cloud formations and better color gradients. The lighting on the mountain peaks is more realistic. Which version do you prefer?

**Complexity**: **Low** — Wrappers around ElBruno.Text2Image API. Prompt handling, file I/O, model selection logic. ~40 lines of tool code beyond the template.

**External dependencies**: `ElBruno.Text2Image.Cuda` (or `.Foundry` for FLUX.2 cloud). Both available via NuGet.

---

## Scenario 5: Style Transfer Pipeline Agent

**One-line description**: Describe a scene, then ask the agent to generate artistic variations in different styles — oil painting, cyberpunk, watercolor, anime.

**Why it's useful**: Creative workflows often require seeing the same concept in multiple artistic styles before committing to a direction. Generating 4-6 variations manually takes minutes even with local tools. A hosted GPU agent can orchestrate multi-model inference server-side: generate base scene, apply style modifiers, return all variations in ~8 seconds. This demonstrates the agent's orchestration capability — the LLM decides which styles to try and interprets user feedback ("more mist in the watercolor version"), while the GPU executes the rendering pipeline.

**Why GPU**: Generating multiple style variations requires sequential diffusion model inference. 4 variations × ~2s each = 8s on GPU vs 2+ minutes on CPU.

**Function tools**:

```csharp
[Description("Generates an image from a text description in a specific artistic style")]
static async Task<string> GenerateStyledImage(
    [Description("Base scene description")] string prompt,
    [Description("Artistic style: 'oil painting', 'cyberpunk neon', 'watercolor', 'anime', 'pixel art', 'photorealistic'")] string style,
    [Description("Optional seed for reproducibility")] int? seed = null)

[Description("Generates a batch of 4 images with different styles from the same base description for comparison")]
static async Task<string> GenerateStyleComparison(
    [Description("Base scene description")] string basePrompt,
    [Description("Comma-separated styles to compare (max 4)")] string styles)

[Description("Adjusts generation parameters for quality vs speed tradeoff")]
static string SetQualityPreset(
    [Description("Preset: 'draft' (fast, lower quality), 'standard', 'high' (slower, best quality)")] string preset)
```

**Example conversation**:

> **User**: I need to visualize a robot learning to paint in a studio.
>
> **Agent**: Great concept! Let me generate 4 style variations so you can see how it looks in different artistic approaches. I'll try: oil painting, cyberpunk neon, watercolor, and anime.
>
> *(7.8 seconds later)*
>
> ✅ Done! Generated 4 variations:
>
> | Style | File | Notes |
> |-------|------|-------|
> | 🎨 Oil painting | `output/robot_studio_oil_20260312_144501.png` | Classical brushwork, warm lighting |
> | 🌃 Cyberpunk neon | `output/robot_studio_cyber_20260312_144503.png` | Neon accents, futuristic aesthetic |
> | 💧 Watercolor | `output/robot_studio_water_20260312_144505.png` | Soft edges, translucent washes |
> | 🎌 Anime | `output/robot_studio_anime_20260312_144507.png` | Bold outlines, expressive pose |
>
> Which style direction resonates with you?
>
> **User**: The watercolor is close, but I want more mist and softer lighting.
>
> **Agent**: I'll refine the watercolor version with those adjustments...
>
> *(2.1 seconds later)*
>
> ✅ Done! Generated: `output/robot_studio_water_refined_20260312_144512.png`
>
> Added atmospheric mist around the robot and softened the light source. The overall effect is more dreamlike. How's this?

**Complexity**: **Medium** — Style prompt engineering (~30 lines), batch generation with async processing (~30 lines), quality preset management (~20 lines). Total ~80 lines of tool code.

**External dependencies**: `ElBruno.Text2Image.Cuda`, optionally `SixLabors.ImageSharp` for post-processing (resizing, watermarking).

---

## Scenario 6: Storyboard Generator Agent

**One-line description**: Describe a story concept, and the agent generates a 6-frame visual storyboard with scene descriptions, mood notes, and camera direction.

**Why it's useful**: Visual storytelling requires coherence across multiple frames — consistent characters, lighting, environment, and narrative flow. This is exactly the kind of multi-step orchestration that hosted agents excel at: the LLM breaks down the story premise into scenes, generates detailed prompts for each frame (maintaining visual consistency), orchestrates GPU rendering, and assembles the results into a structured storyboard. Doing this manually requires careful prompt engineering and 3-5 minutes of generation time. The agent does it in 10-15 seconds.

**Why GPU**: Generating 6-8 coherent images requires 10-15 seconds of GPU time. On CPU, this would take 3-5 minutes, destroying demo interactivity. Consistency across frames (same character, lighting, environment) requires careful prompt engineering that the LLM orchestrates while the GPU renders.

**Function tools**:

```csharp
[Description("Generates a visual storyboard (6 frames) from a story premise. Returns frame images with scene descriptions.")]
static async Task<string> GenerateStoryboard(
    [Description("Title or premise of the story")] string premise,
    [Description("Visual style: 'cinematic', 'animated', 'watercolor', 'comic book'")] string style = "cinematic",
    [Description("Number of frames (4-8)")] int frameCount = 6)

[Description("Regenerates a specific frame with adjusted details while maintaining visual consistency")]
static async Task<string> RefineFrame(
    [Description("Frame number to refine (1-8)")] int frameNumber,
    [Description("Original frame description for context")] string originalDescription,
    [Description("What to change: composition, mood, lighting, or detail adjustments")] string refinement)

[Description("Exports the storyboard as a markdown or HTML document with all frames, descriptions, and production notes")]
static string ExportStoryboard(
    [Description("Completed frame data as JSON array")] string frameDataJson,
    [Description("Output format: 'markdown' or 'html'")] string format = "markdown")
```

**Example conversation**:

> **User**: Create a storyboard for a short film about an AI learning to create art.
>
> **Agent**: Perfect! I'll generate a 6-frame cinematic storyboard exploring that premise. This will take about 12 seconds to render on GPU...
>
> *(11.7 seconds later)*
>
> ✅ Done! Generated storyboard: `output/storyboard_ai_artist_20260312_145023/`
>
> | Frame | Scene | Mood | Shot |
> |-------|-------|------|------|
> | **1** | AI wakes up in a pristine lab, screens glowing | Curious, sterile | Wide shot, cool blue lighting |
> | **2** | AI discovers a paint-splattered art studio next door | Wonder, discovery | Medium shot through doorway |
> | **3** | First attempt: AI holds brush awkwardly, paint drips | Frustration, determination | Close-up on robotic hand |
> | **4** | Montage: failed canvases pile up, AI studies technique | Struggle, growth | Time-lapse feel, warm lighting |
> | **5** | Breakthrough moment: AI creates abstract piece | Joy, epiphany | Over-shoulder shot of canvas |
> | **6** | Exhibition: AI stands beside finished artwork, humans admire | Pride, connection | Wide shot, golden hour light |
>
> Each frame maintains consistent character design (white chassis, blue optical sensors) and lighting continuity. Want to refine any frame?
>
> **User**: Frame 3 feels too dark. Can you brighten it and show more of the studio in the background?
>
> **Agent**: I'll regenerate Frame 3 with better lighting and a wider composition to show the studio context...
>
> *(2.4 seconds later)*
>
> ✅ Done! Updated `frame_03_revised.png`
>
> Now you can see paintbrushes and canvases on shelves behind the AI, and the lighting is warmer and more inviting. The frustration is still visible in the pose, but the environment feels less isolating.
>
> **User**: Perfect. Export this as HTML.
>
> **Agent**: Exported! Saved to `output/storyboard_ai_artist_20260312_145023/storyboard.html`
>
> The HTML includes all 6 frames with scene descriptions, mood notes, camera direction, and production notes. Ready to share or present.

**Complexity**: **Advanced** — Story premise parsing and scene decomposition (~40 lines), prompt consistency logic for character/lighting/style (~50 lines), batch rendering with progress tracking (~30 lines), export formatting (~30 lines). Total ~150 lines of tool code.

**External dependencies**: `ElBruno.Text2Image.Cuda` (SDXL Turbo for better consistency), `SixLabors.ImageSharp` for image processing and layout.

---

## Comparison Matrix

| | Code Metrics | Data Crunch | Secret Scanner | Image Generator | Style Transfer | Storyboard |
|---|---|---|---|---|---|---|
| **Tools** | 3 | 3 | 3 | 3 | 3 | 3 |
| **Lines of tool code** | ~60 | ~90 | ~100 | ~40 | ~80 | ~150 |
| **External deps** | None | None | None | ElBruno.Text2Image | ElBruno.Text2Image | ElBruno.Text2Image |
| **GPU required** | No | No | No | ✅ Yes | ✅ Yes | ✅ Yes |
| **Complexity** | Low | Low–Medium | Low–Medium | Low | Medium | Advanced |
| **Demo appeal** | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **"Why not just prompt?"** | LLMs guess at metrics | LLMs can't do math | Regex is deterministic | LLMs can't generate images | Multi-model GPU pipeline | Coherent visual sequences |
| **Audience** | Developers | Data-curious devs | Security-aware devs | Creative devs | Designers & devs | Content creators |

## Recommendation

**Start with Scenario 2 (Data Crunch Agent)**. It has the strongest "wow" moment in a demo — paste data, get real computed statistics that the model literally cannot do on its own. The math is visually verifiable ("Is the median right? Yes."), which builds trust. It's also the most universally relatable scenario: everyone has data they need to understand.

**Scenario 3 (Secret Scanner)** is the best second pick — security resonates with every audience, and the before/after redaction is a satisfying visual.

**For GPU-powered demos, start with Scenario 4 (Image Generator)**. It's the simplest entry point to GPU agents — a single tool call produces a visible image in under 2 seconds. Then scale up to **Scenario 5 (Style Transfer)** for developer audiences who want to see multi-model orchestration, or **Scenario 6 (Storyboard)** for leadership demos showing the full creative potential of GPU-backed hosted agents.
