using Brinell.Scraper.Models;

namespace Brinell.Scraper.Services;

public interface IControlRegistry
{
    IReadOnlyList<GeneratedControl> GetAllControls();
    GeneratedControl? GetControl(string name);
    void StoreControl(GeneratedControl control);
    void DeleteControl(string name);
}
