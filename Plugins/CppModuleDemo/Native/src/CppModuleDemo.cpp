#include "CppModuleDemo.h"

#include <algorithm>
#include <array>
#include <atomic>
#include <cstring>
#include <string>

namespace
{
std::atomic<ManagedCallback> g_managedCallback{nullptr};
std::atomic<int> g_managedCallbackInvocationCount{0};
}

extern "C" CPP_MODULE_DEMO_API void modifyComplexObject(ExampleComplexObject* obj)
{
    if (obj == nullptr)
    {
        return;
    }

    obj->integerValue += 42;
    obj->doubleValue *= 1.5;

    std::string source = obj->stringValue;
    std::string suffix = " [updated by C++ demo]";
    std::string updated = source + suffix;

    std::array<char, 256> output{};
    std::size_t copyLength = std::min<std::size_t>(updated.size(), output.size() - 1);
    std::memcpy(output.data(), updated.data(), copyLength);
    output[copyLength] = '\0';

    std::memcpy(obj->stringValue, output.data(), output.size());
}

extern "C" CPP_MODULE_DEMO_API void setManagedCallback(ManagedCallback callback)
{
    g_managedCallback.store(callback);
}

extern "C" CPP_MODULE_DEMO_API int invokeManagedCallback()
{
    ManagedCallback callback = g_managedCallback.load();
    if (callback == nullptr)
    {
        return 0;
    }

    callback();
    return ++g_managedCallbackInvocationCount;
}
