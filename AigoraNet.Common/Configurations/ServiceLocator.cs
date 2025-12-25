using Microsoft.Extensions.DependencyInjection;
using System;

namespace AigoraNet.Common.Helpers;

// 💡 Service Locator 패턴의 핵심 클래스
public static class ServiceLocator
{
    // DI 컨테이너의 IServiceProvider 인스턴스를 저장할 정적 필드
    private static IServiceProvider? _serviceProvider;

    /// <summary>
    /// DI 컨테이너가 빌드된 후 IServiceProvider를 설정합니다.
    /// </summary>
    public static void SetLocatorProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// 설정된 IServiceProvider를 통해 서비스를 Resolve(조회)합니다.
    /// </summary>
    /// <typeparam name="T">조회할 서비스의 타입 (인터페이스)</typeparam>
    /// <returns>서비스의 인스턴스</returns>
    public static T? Resolve<T>() where T : class
    {
        // GetRequiredService<T>()를 사용하여 서비스가 등록되지 않았을 경우 예외 발생
        return _serviceProvider?.GetRequiredService<T>();
    }
}