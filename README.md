# N.I.C.E. - Nextspace Intelligent Combo Evaluator
<img width="1024" height="1024" alt="nicelogo" src="https://github.com/user-attachments/assets/cfe4f65c-5ecc-4982-9591-b8326cbc778b" />

N.I.C.E. is a high-performance solver designed to identify the most powerful tag combinations based on a complex scoring and incompatibility matrix. It provides a complete suite for managing tag attributes, enforcing rules, and executing massive computations.

## 🚀 Key Features

### 1. High-Performance Solver
* **Massive Combinatorial Analysis**: Evaluates billions of potential synergies across a 512-tag universe using an optimized bitmask-based engine.
* **Combo Sizes**: Supports deep analysis for combinations of 2 to 5 tags.
* **Top-N Mode**: Quickly identifies and displays the top results based on the highest potential scores.
* **Disjoint Mode**: A specialized computation mode ensuring each tag appears only once across all results. 
  > **⚠️ Disclaimer**: Due to the nature of Disjoint Mode, computation can be extremely demanding. Depending on your configuration and hardware (CPU/RAM), the process may take several hours and consume significant memory.

### 2. T.A.M. (Tag Attribute Manager)
* **Metadata Control**: Full management of tag names, descriptions, and special properties.
* **Rarity Scaling**: Adjust rarity levels to automatically update base score multipliers.
* **Score Calculation**: Automatically computes `MaxPotentialScore` based on current attributes.

### 3. T.I.M. (Tag Incompatibility Manager)
* **Rule Enforcement**: Define exclusion rules between tags to ensure logical consistency.
* **Live Masking**: Updates the solver's 512-bit incompatibility matrix in real-time.

## 📂 Data & Logs

* `NICE.exe`: Main application.
* `latest.log`: Stores detailed telemetry from the **latest Disjoint computation session only**.
* `/Data/`:
    * `tags.json`: Core tag definitions.
    * `user_overrides.json`: Your custom TAM/TIM modifications.
    * `ComputationRecords.json`: Saved results and session history.

## 📦 Installation

1. **Prerequisites**: Requires [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) or higher.
2. **Installation**: Extract the `.zip` to a folder. Avoid `C:\Program Files\` to ensure the app can write to its `/Data/` folder without restriction.
3. **Run**: Launch `NICE.exe`.

## ⚖️ License

This project is distributed under the **Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International (CC BY-NC-SA 4.0)** license.

* **BY (Attribution)**: Credit the original author (**Nyu**).
* **NC (Non-Commercial)**: No commercial use permitted.
* **SA (ShareAlike)**: Modifications must be shared under the same license.

---
*Developed by Nyu — Support the project on [Buy Me a Coffee](https://buymeacoffee.com/nyunyu)*
*Rocket animation created by SVGator - check their work on [svgator.com](https://www.svgator.com/)*
