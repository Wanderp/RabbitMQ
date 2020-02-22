namespace RabbitMq.Test.Api.Models
{
    public class Counter
    {
        public int ActualValue { get; private set; }

        public void Increment()
        {
            ActualValue++;
        }
    }
}