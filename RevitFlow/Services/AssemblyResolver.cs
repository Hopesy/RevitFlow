using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace RevitFlow.Services
{
    public static class AssemblyResolver
    {
        private static ILogger? _logger;

        public static void Initialize(ILogger logger)
        {
            _logger = logger;
            // AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            _logger.LogInformation("程序集解析器已初始化");
        }

        private static Assembly? OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                // 获取请求加载的程序集名称
                var assemblyName = new AssemblyName(args.Name);
                _logger?.LogInformation($"尝试解析程序集: {assemblyName.Name}");

                // 获取当前执行程序集的目录
                var executingAssemblyPath = Assembly.GetExecutingAssembly().Location;
                var executingDirectory = Path.GetDirectoryName(executingAssemblyPath);

                if (string.IsNullOrEmpty(executingDirectory))
                {
                    _logger?.LogError("无法确定执行目录");
                    return null;
                }

                // 构建可能的DLL路径
                var dllPath = Path.Combine(executingDirectory, $"{assemblyName.Name}.dll");

                // 检查文件是否存在
                if (File.Exists(dllPath))
                {
                    _logger?.LogInformation($"已找到并加载程序集: {dllPath}");
                    return Assembly.LoadFrom(dllPath);
                }

                _logger?.LogWarning($"未能解析程序集: {assemblyName.Name}");
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"解析程序集时发生错误: {ex.Message}");
                return null;
            }
        }

        public static void Cleanup()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
            _logger?.LogInformation("程序集解析器已清理");
        }
    }
}