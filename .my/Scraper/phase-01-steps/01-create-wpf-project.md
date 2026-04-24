# Step 1.1 — Create `Brinell.Scraper` WPF Project (.NET 10)

## Objective

Create the foundational WPF application project targeting .NET 10.

## Implementation

- Create a new WPF Application project targeting `net10.0-windows`
- Project file (`Brinell.Scraper.csproj`):
  ```xml
  <Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
      <OutputType>WinExe</OutputType>
      <TargetFramework>net10.0-windows</TargetFramework>
      <UseWPF>true</UseWPF>
      <Nullable>enable</Nullable>
      <ImplicitUsings>enable</ImplicitUsings>
    </PropertyGroup>
  </Project>
  ```
- Project structure:
  ```
  Brinell.Scraper/
    App.xaml / App.xaml.cs
    MainWindow.xaml / MainWindow.xaml.cs
    ViewModels/
    Views/
    Services/
    Models/
    Converters/
    Resources/
    Data/          # Corpus SQLite access, repositories
    Corpus/        # Corpus services (page tracking, control registry)
  ```
- `App.xaml.cs` — application entry point, DI container bootstrap (step 1.2)
- `MainWindow.xaml` — top-level shell with menu bar, content area, status bar

## Checklist

- [ ] Project created with correct TFM and settings
- [ ] Folder structure in place
- [ ] App launches (empty window)
- [ ] Solution builds without errors
