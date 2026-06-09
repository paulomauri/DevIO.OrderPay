namespace DevIO.OrderPay.Customer.Exceptions;

public class DuplicateCpfException(string cpf) : Exception($"CPF '{cpf}' is already registered.")
{
}
