using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

public class Worker
{
	private Worker<object, object, object, object> w;
	private void w_Work(object arg1, object arg2, object arg3, object arg4)
	{
		if (Work != null)
			Work();
	}

	public Worker() : this(1) { }
	public Worker(int WorkerMax)
	{
		w = new Worker<object, object, object, object>(WorkerMax);
		w.Work += w_Work;
	}
	public event Action Work;
	public void Do()
	{
		w.Do(null, null, null, null);
	}
}
public class Worker<T1>
{
	private Worker<T1, object, object, object> w;
	private void w_Work(T1 arg1, object arg2, object arg3, object arg4)
	{
		if (Work != null)
			Work(arg1);
	}

	public Worker() : this(1) { }
	public Worker(int WorkerMax)
	{
		w = new Worker<T1, object, object, object>(WorkerMax);
		w.Work += w_Work;
	}
	public event Action<T1> Work;
	public void Do(T1 o1)
	{
		w.Do(o1, null, null, null);
	}
}
public class Worker<T1, T2>
{
	private Worker<T1, T2, object, object> w;
	private void w_Work(T1 arg1, T2 arg2, object arg3, object arg4)
	{
		if (Work != null)
			Work(arg1, arg2);
	}

	public Worker() : this(1) { }
	public Worker(int WorkerMax)
	{
		w = new Worker<T1, T2, object, object>(WorkerMax);
		w.Work += w_Work;
	}
	public event Action<T1, T2> Work;
	public void Do(T1 o1, T2 o2)
	{
		w.Do(o1, o2, null, null);
	}
}
public class Worker<T1, T2, T3>
{
	private Worker<T1, T2, T3, object> w;
	private void w_Work(T1 arg1, T2 arg2, T3 arg3, object arg4)
	{
		if (Work != null)
			Work(arg1, arg2, arg3);
	}

	public Worker() : this(1) { }
	public Worker(int WorkerMax)
	{
		w = new Worker<T1, T2, T3, object>(WorkerMax);
		w.Work += w_Work;
	}
	public event Action<T1, T2, T3> Work;
	public void Do(T1 o1, T2 o2, T3 o3)
	{
		w.Do(o1, o2, o3, null);
	}
}
public class Worker<T1, T2, T3, T4>
{
	private class Task
	{
		public T1 o1;
		public T2 o2;
		public T3 o3;
		public T4 o4;
	}

	private int WorkerMax;
	private int WorkerCount = 0;
	private Queue TaskList = new Queue();

	public Worker() : this(1) { }
	public Worker(int WorkerMax)
	{
		this.WorkerMax = WorkerMax > 0 ? WorkerMax : 1;
		TaskRunnerDelegate = new Action(TaskRunner);
	}

	private Action TaskRunnerDelegate;
	private void TaskRunner()
	{
		while (true)
		{
			Task t = null;
			bool Available = false;
			lock (TaskList.SyncRoot)
			{
				if (TaskList.Count > 0)
				{
					t = (Task)TaskList.Dequeue();
					Available = true;
				}
				else
				{
					WorkerCount--;
					return;
				}
			}

			if (Available)
			{
				if (Work != null)
					Work.Invoke(t.o1, t.o2, t.o3, t.o4);
			}
		}
	}

	public event Action<T1, T2, T3, T4> Work;
	public void Do(T1 o1, T2 o2, T3 o3, T4 o4)
	{
		Task t = new Task();
		t.o1 = o1;
		t.o2 = o2;
		t.o3 = o3;
		t.o4 = o4;

		lock (TaskList.SyncRoot)
		{
			TaskList.Enqueue(t);

			if (WorkerCount < WorkerMax)
			{
				TaskRunnerDelegate.BeginInvoke(null, null);
				WorkerCount++;
			}
		}
	}
}
