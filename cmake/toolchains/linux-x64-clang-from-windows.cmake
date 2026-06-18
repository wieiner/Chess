# Draft toolchain for a future P4D Linux-native Chess3D authority spike.
#
# This file documents the intended Windows-hosted Clang path. It does not prove
# that the repository can cross-compile to Linux yet. A Linux sysroot is still
# required before any real linux-x64 native authority build can be trusted.

set(CMAKE_SYSTEM_NAME Linux)
set(CMAKE_SYSTEM_PROCESSOR x86_64)

set(CHESS_LLVM_ROOT "C:/ll/local" CACHE PATH "Local LLVM root used for the P4D spike")
set(CHESS_LINUX_TARGET "x86_64-linux-gnu" CACHE STRING "Linux target triple")

set(CMAKE_C_COMPILER "${CHESS_LLVM_ROOT}/bin/clang.exe")
set(CMAKE_CXX_COMPILER "${CHESS_LLVM_ROOT}/bin/clang++.exe")
set(CMAKE_C_COMPILER_TARGET "${CHESS_LINUX_TARGET}")
set(CMAKE_CXX_COMPILER_TARGET "${CHESS_LINUX_TARGET}")

set(CMAKE_AR "${CHESS_LLVM_ROOT}/bin/llvm-ar.exe")
set(CMAKE_RANLIB "${CHESS_LLVM_ROOT}/bin/llvm-ranlib.exe")
set(CMAKE_LINKER "${CHESS_LLVM_ROOT}/bin/ld.lld.exe")

if(DEFINED ENV{LINUX_SYSROOT} AND NOT "$ENV{LINUX_SYSROOT}" STREQUAL "")
    file(TO_CMAKE_PATH "$ENV{LINUX_SYSROOT}" CHESS_LINUX_SYSROOT)
elseif(DEFINED ENV{SYSROOT} AND NOT "$ENV{SYSROOT}" STREQUAL "")
    file(TO_CMAKE_PATH "$ENV{SYSROOT}" CHESS_LINUX_SYSROOT)
else()
    set(CHESS_LINUX_SYSROOT "" CACHE PATH "Linux sysroot path for cross-compilation")
endif()

if(CHESS_LINUX_SYSROOT)
    set(CMAKE_SYSROOT "${CHESS_LINUX_SYSROOT}")
    set(CMAKE_FIND_ROOT_PATH "${CHESS_LINUX_SYSROOT}")
else()
    message(WARNING "No Linux sysroot configured. This toolchain is a P4D draft only; real Linux linking is expected to fail until CMAKE_SYSROOT or LINUX_SYSROOT is set.")
endif()

set(CMAKE_TRY_COMPILE_TARGET_TYPE STATIC_LIBRARY)

set(CMAKE_FIND_ROOT_PATH_MODE_PROGRAM NEVER)
set(CMAKE_FIND_ROOT_PATH_MODE_LIBRARY ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_INCLUDE ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_PACKAGE ONLY)
