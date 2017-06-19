#ifdef WIN32
    #define EXPORT_FUNC __declspec(dllexport)
#else
    #define EXPORT_FUNC
#endif

extern "C" EXPORT_FUNC int add_two_nums(int a, int b)
{
    return a + b;
}
