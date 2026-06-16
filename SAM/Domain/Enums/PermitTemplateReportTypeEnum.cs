namespace SAM.Domain.Enums;

[Flags]
public enum PermitTemplateReportTypeEnum
{
    None = 0,
    Ndmr = 1,
    Nvmr = 2,
    Gw59 = 4,
    Gw59A = 8
}

