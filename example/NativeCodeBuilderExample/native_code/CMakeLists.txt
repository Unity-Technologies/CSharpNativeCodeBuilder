CMAKE_MINIMUM_REQUIRED(VERSION 2.8)

PROJECT(ExampleNativeCode)

add_library(ExampleNativeCode SHARED lib.cpp)

install (TARGETS ExampleNativeCode
         ARCHIVE DESTINATION lib
         LIBRARY DESTINATION lib
         RUNTIME DESTINATION lib)
