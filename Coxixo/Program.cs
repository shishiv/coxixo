namespace Coxixo;

static class Program
{
    private static Mutex? _mutex;

    [STAThread]
    static void Main()
    {
        const string mutexName = "Global\\CoxixoSingleInstance";
        _mutex = new Mutex(true, mutexName, out bool createdNew);

        if (!createdNew)
        {
            MessageBox.Show("Coxixo is already running.", "Coxixo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext());
    }
}