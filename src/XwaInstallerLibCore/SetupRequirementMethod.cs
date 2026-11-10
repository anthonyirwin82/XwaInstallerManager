namespace XwaInstallerLibCore;

public enum SetupRequirementMethod
{
    FileMustExist,
    FileMustNotExist,
    AllLinesMustExist,
    AllLinesMustNotExist,
    OneLineMustExist,
    OneLineMustNotExist,
    AllLinesMustMatch,
    AllLinesMustNotMatch,
    OneLineMustMatch,
    OneLineMustNotMatch
}
