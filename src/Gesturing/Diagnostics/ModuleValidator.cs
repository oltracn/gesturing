using System.Text.Json;
using Gesturing.Models;
using Gesturing.Services;

namespace Gesturing.Diagnostics;

public static class ModuleValidator
{
    public static async Task<bool> RunAllTestsAsync()
    {
        Console.WriteLine("=== Gesturing 模块验证 ===\n");

        var allPassed = true;

        allPassed &= TestConfigModelSerialization();
        allPassed &= TestConfigPathResolver();
        allPassed &= TestDefaultConfigValues();
        allPassed &= TestProcessHelper();

        Console.WriteLine($"\n=== 验证结果: {(allPassed ? "✅ 全部通过" : "❌ 存在失败")} ===");
        await Task.Delay(2000);
        return allPassed;
    }

    private static bool TestConfigModelSerialization()
    {
        Console.WriteLine("[测试 1] 配置模型 JSON 序列化...");

        try
        {
            var config = DefaultConfig.Create();
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            Console.WriteLine("  生成的 JSON:");
            Console.WriteLine($"  {json.Replace("\n", "\n  ")}");

            var deserialized = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (deserialized == null)
            {
                Console.WriteLine("  ❌ 反序列化返回 null");
                return false;
            }

            if (deserialized.TargetProcesses.Count != config.TargetProcesses.Count)
            {
                Console.WriteLine("  ❌ TargetProcesses 数量不匹配");
                return false;
            }

            if (deserialized.Rules.Count != config.Rules.Count)
            {
                Console.WriteLine("  ❌ Rules 数量不匹配");
                return false;
            }

            Console.WriteLine("  ✅ 通过");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ 异常: {ex.Message}");
            return false;
        }
    }

    private static bool TestConfigPathResolver()
    {
        Console.WriteLine("\n[测试 2] 配置路径解析器...");

        try
        {
            var isPortable = ConfigPathResolver.IsPortableMode();
            Console.WriteLine($"  便携模式: {isPortable}");

            var configDir = ConfigPathResolver.GetConfigDirectory();
            Console.WriteLine($"  配置目录: {configDir}");

            var configPath = ConfigPathResolver.GetConfigPath();
            Console.WriteLine($"  配置文件路径: {configPath}");

            if (!Directory.Exists(configDir))
            {
                Console.WriteLine("  ❌ 配置目录不存在");
                return false;
            }

            Console.WriteLine("  ✅ 通过");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ 异常: {ex.Message}");
            return false;
        }
    }

    private static bool TestDefaultConfigValues()
    {
        Console.WriteLine("\n[测试 3] 默认配置值验证...");

        try
        {
            var config = DefaultConfig.Create();

            Console.WriteLine($"  AutoStart: {config.AutoStart}");
            Console.WriteLine($"  IsEnabled: {config.IsEnabled}");
            Console.WriteLine($"  白名单进程: {string.Join(", ", config.TargetProcesses)}");
            Console.WriteLine($"  手势规则数: {config.Rules.Count}");

            foreach (var rule in config.Rules)
            {
                var gesture = string.Join(" → ", rule.GestureSequence);
                var modifiers = string.Join("+", rule.Hotkey.Modifiers);
                Console.WriteLine($"    - {gesture} : {modifiers}{(modifiers.Length > 0 ? "+" : "")}{rule.Hotkey.Key}");
            }

            if (!config.TargetProcesses.Contains("chrome.exe"))
            {
                Console.WriteLine("  ❌ 默认白名单缺少 chrome.exe");
                return false;
            }

            if (config.Rules.Count < 3)
            {
                Console.WriteLine("  ❌ 默认规则数不足");
                return false;
            }

            Console.WriteLine("  ✅ 通过");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ 异常: {ex.Message}");
            return false;
        }
    }

    private static bool TestProcessHelper()
    {
        Console.WriteLine("\n[测试 4] 进程查询工具...");

        try
        {
            var processName = ProcessHelper.GetForegroundProcessName();
            Console.WriteLine($"  当前前台进程: {processName ?? "null"}");

            if (string.IsNullOrEmpty(processName))
            {
                Console.WriteLine("  ⚠️  无法获取前台进程名（可能是系统窗口）");
            }
            else
            {
                Console.WriteLine("  ✅ 进程查询成功");
            }

            ProcessHelper.ClearCache();
            Console.WriteLine("  ✅ 缓存清理成功");

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ 异常: {ex.Message}");
            return false;
        }
    }
}
