namespace DiningPhilosophers.Models;

public class Fork
{
	public int ID { get; }
	public object LockObject { get; } = new object();

	public Fork(int id)
	{
		ID = id;
	}
}
