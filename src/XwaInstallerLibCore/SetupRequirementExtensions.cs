using System.Text;
using System.Text.RegularExpressions;

namespace XwaInstallerLibCore;

public static class SetupRequirementExtensions
{
    private static readonly Encoding _encoding = GetEncoding(1252);

    private static Encoding GetEncoding(int codePage)
    {
        Platform.EncodingSetup.EnsureRegistered();
        return Encoding.GetEncoding(codePage);
    }

    private static Encoding GetEncoding(string name)
    {
        Platform.EncodingSetup.EnsureRegistered();
        return Encoding.GetEncoding(name);
    }

    public static bool IsValid(this SetupRequirement requirement, string folder)
    {
        if (!Directory.Exists(folder))
        {
            return false;
        }

        return requirement.Method switch
        {
            SetupRequirementMethod.FileMustExist => CheckFileMustExist(requirement, folder),
            SetupRequirementMethod.FileMustNotExist => CheckFileMustNotExist(requirement, folder),
            SetupRequirementMethod.AllLinesMustExist => CheckAllLinesMustExist(requirement, folder),
            SetupRequirementMethod.AllLinesMustNotExist => CheckAllLinesMustNotExist(requirement, folder),
            SetupRequirementMethod.OneLineMustExist => CheckOneLineMustExist(requirement, folder),
            SetupRequirementMethod.OneLineMustNotExist => CheckOneLineMustNotExist(requirement, folder),
            SetupRequirementMethod.AllLinesMustMatch => CheckAllLinesMustMatch(requirement, folder),
            SetupRequirementMethod.AllLinesMustNotMatch => CheckAllLinesMustNotMatch(requirement, folder),
            SetupRequirementMethod.OneLineMustMatch => CheckOneLineMustMatch(requirement, folder),
            SetupRequirementMethod.OneLineMustNotMatch => CheckOneLineMustNotMatch(requirement, folder),
            _ => false
        };
    }

    private static bool CheckFileMustExist(SetupRequirement requirement, string folder)
    {
        return File.Exists(Path.Combine(folder, requirement.FileName));
    }

    private static bool CheckFileMustNotExist(SetupRequirement requirement, string folder)
    {
        return !File.Exists(Path.Combine(folder, requirement.FileName));
    }

    private static bool CheckAllLinesMustExist(SetupRequirement requirement, string folder)
    {
        if (requirement.Parameters.Count == 0)
        {
            return true;
        }

        string path = Path.Combine(folder, requirement.FileName);

        if (!File.Exists(path))
        {
            return false;
        }

        string[] array = File.ReadAllLines(path, _encoding);

        if (array.Length == 0)
        {
            return false;
        }

        bool result = true;

        foreach (string parameter in requirement.Parameters)
        {
            if (!array.Contains(parameter, StringComparer.OrdinalIgnoreCase))
            {
                result = false;
                break;
            }
        }

        return result;
    }

    private static bool CheckAllLinesMustNotExist(SetupRequirement requirement, string folder)
    {
        if (requirement.Parameters.Count == 0)
        {
            return true;
        }

        string path = Path.Combine(folder, requirement.FileName);

        if (!File.Exists(path))
        {
            return true;
        }

        string[] array = File.ReadAllLines(path, _encoding);

        if (array.Length == 0)
        {
            return true;
        }

        bool result = true;

        foreach (string parameter in requirement.Parameters)
        {
            if (array.Contains(parameter, StringComparer.OrdinalIgnoreCase))
            {
                result = false;
                break;
            }
        }

        return result;
    }

    private static bool CheckOneLineMustExist(SetupRequirement requirement, string folder)
    {
        if (requirement.Parameters.Count == 0)
        {
            return true;
        }

        string path = Path.Combine(folder, requirement.FileName);

        if (!File.Exists(path))
        {
            return false;
        }

        string[] array = File.ReadAllLines(path, _encoding);

        if (array.Length == 0)
        {
            return false;
        }

        bool result = false;

        foreach (string parameter in requirement.Parameters)
        {
            if (array.Contains(parameter, StringComparer.OrdinalIgnoreCase))
            {
                result = true;
                break;
            }
        }

        return result;
    }

    private static bool CheckOneLineMustNotExist(SetupRequirement requirement, string folder)
    {
        if (requirement.Parameters.Count == 0)
        {
            return true;
        }

        string path = Path.Combine(folder, requirement.FileName);

        if (!File.Exists(path))
        {
            return true;
        }

        string[] array = File.ReadAllLines(path, _encoding);

        if (array.Length == 0)
        {
            return true;
        }

        bool result = false;

        foreach (string parameter in requirement.Parameters)
        {
            if (!array.Contains(parameter, StringComparer.OrdinalIgnoreCase))
            {
                result = true;
                break;
            }
        }

        return result;
    }

    private static bool CheckAllLinesMustMatch(SetupRequirement requirement, string folder)
    {
        if (requirement.Parameters.Count == 0)
        {
            return true;
        }

        string path = Path.Combine(folder, requirement.FileName);

        if (!File.Exists(path))
        {
            return false;
        }

        string[] array = File.ReadAllLines(path, _encoding);

        if (array.Length == 0)
        {
            return false;
        }

        bool result = true;

        foreach (string parameter in requirement.Parameters)
        {
            try
            {
                var regex = new Regex(parameter, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                bool flag = false;

                foreach (string input in array)
                {
                    if (regex.IsMatch(input))
                    {
                        flag = true;
                        break;
                    }
                }

                if (!flag)
                {
                    result = false;
                    break;
                }
            }
            catch
            {
                result = false;
                break;
            }
        }

        return result;
    }

    private static bool CheckAllLinesMustNotMatch(SetupRequirement requirement, string folder)
    {
        if (requirement.Parameters.Count == 0)
        {
            return true;
        }

        string path = Path.Combine(folder, requirement.FileName);

        if (!File.Exists(path))
        {
            return true;
        }

        string[] array = File.ReadAllLines(path, _encoding);

        if (array.Length == 0)
        {
            return true;
        }

        bool result = true;

        foreach (string parameter in requirement.Parameters)
        {
            try
            {
                var regex = new Regex(parameter, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                bool flag = false;

                foreach (string input in array)
                {
                    if (regex.IsMatch(input))
                    {
                        flag = true;
                        break;
                    }
                }

                if (flag)
                {
                    result = false;
                    break;
                }
            }
            catch
            {
                result = false;
                break;
            }
        }

        return result;
    }

    private static bool CheckOneLineMustMatch(SetupRequirement requirement, string folder)
    {
        if (requirement.Parameters.Count == 0)
        {
            return true;
        }

        string path = Path.Combine(folder, requirement.FileName);

        if (!File.Exists(path))
        {
            return false;
        }

        string[] array = File.ReadAllLines(path, _encoding);

        if (array.Length == 0)
        {
            return false;
        }

        bool result = false;

        foreach (string parameter in requirement.Parameters)
        {
            try
            {
                var regex = new Regex(parameter, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                bool flag = false;

                foreach (string input in array)
                {
                    if (regex.IsMatch(input))
                    {
                        flag = true;
                        break;
                    }
                }

                if (flag)
                {
                    result = true;
                    break;
                }
            }
            catch
            {
                result = false;
                break;
            }
        }

        return result;
    }

    private static bool CheckOneLineMustNotMatch(SetupRequirement requirement, string folder)
    {
        if (requirement.Parameters.Count == 0)
        {
            return true;
        }

        string path = Path.Combine(folder, requirement.FileName);

        if (!File.Exists(path))
        {
            return true;
        }

        string[] array = File.ReadAllLines(path, _encoding);

        if (array.Length == 0)
        {
            return true;
        }

        bool result = false;

        foreach (string parameter in requirement.Parameters)
        {
            try
            {
                var regex = new Regex(parameter, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                bool flag = false;

                foreach (string input in array)
                {
                    if (regex.IsMatch(input))
                    {
                        flag = true;
                        break;
                    }
                }

                if (flag)
                {
                    result = true;
                    break;
                }
            }
            catch
            {
                result = false;
                break;
            }
        }

        return result;
    }
}
