#pragma once

#ifdef _WIN32
#if defined(CPP_MODULE_DEMO_EXPORTS)
#define CPP_MODULE_DEMO_API __declspec(dllexport)
#else
#define CPP_MODULE_DEMO_API __declspec(dllimport)
#endif
#else
#define CPP_MODULE_DEMO_API
#endif

extern "C" {

struct ExampleComplexObject
{
    int integerValue;
    double doubleValue;
    char stringValue[256];
};

using ManagedCallback = void (*)();

CPP_MODULE_DEMO_API void modifyComplexObject(ExampleComplexObject* obj);
CPP_MODULE_DEMO_API void setManagedCallback(ManagedCallback callback);
CPP_MODULE_DEMO_API int invokeManagedCallback();

}
