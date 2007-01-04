public class MonoDevelopBootstrap
{
    public static void Main()
    {
        Mono.Unix.Native.Syscall.system("XBANSHEE_PROFILES_NO_TEST=1 make run-nocheck");
    }
}
