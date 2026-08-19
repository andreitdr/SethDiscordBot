cmake_minimum_required(VERSION 3.20)
project(CppModuleDemoNative LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

add_library(CppModuleDemo SHARED
    src/CppModuleDemo.cpp
)

target_include_directories(CppModuleDemo
    PUBLIC
        ${CMAKE_CURRENT_SOURCE_DIR}/include
)

if (WIN32)
    target_compile_definitions(CppModuleDemo PRIVATE CPP_MODULE_DEMO_EXPORTS)
endif()
