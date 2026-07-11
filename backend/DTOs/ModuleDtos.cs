namespace BusinessOps.Backend.DTOs;

public class ModuleFieldDto
{
    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;
}

public class CreateModuleRequestDto
{
    public string Name { get; set; } = string.Empty;

    public List<ModuleFieldDto> Fields { get; set; } = new();
}

public class UpdateModuleRequestDto
{
    public string Name { get; set; } = string.Empty;

    public List<ModuleFieldDto> Fields { get; set; } = new();
}
