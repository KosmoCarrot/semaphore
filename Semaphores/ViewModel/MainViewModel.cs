using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System;
using System.ComponentModel;
using System.Threading;
using System.Linq;
using System.Windows;

namespace Semaphores.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private static int _counter = 0;
        private static SemaphoreSlim _semaphore;

        private ICollectionView _createdThreadsView;
        private ICollectionView _waitingThreadsView;
        private ICollectionView _workingThreadsView;
        private SemaphoreThread _currentThread;
        private decimal _semaphoreCapacity;

        public MainViewModel()
        {
            Threads = new ObservableCollection<SemaphoreThread>();

            CreatedThreadsView = new CollectionViewSource { Source = Threads }.View;
            CreatedThreadsView.Filter = (t) => { return (t as SemaphoreThread).Status == Status.Created; };
            WaitingThreadsView = new CollectionViewSource { Source = Threads }.View;
            WaitingThreadsView.Filter = (t) => { return (t as SemaphoreThread).Status == Status.Waiting; };
            WorkingThreadsView = new CollectionViewSource { Source = Threads }.View;
            WorkingThreadsView.Filter = (t) => { return (t as SemaphoreThread).Status == Status.Working; };

            CreateThreadCommand = new RelayCommand(CreateThread);
            MoveThreadCommand = new RelayCommand(MoveThread);

            _semaphore = new SemaphoreSlim(int.Parse(SemaphoreCapacity.ToString()), 100);
        }

        public RelayCommand CreateThreadCommand { get; set; }
        public RelayCommand MoveThreadCommand { get; set; }
        public ObservableCollection<SemaphoreThread> Threads { get; private set; }

        public SemaphoreThread CurrentThread
        {
            get { return _currentThread; }
            set
            {
                _currentThread = value;
                RaisePropertyChanged("CurrentThread");
            }
        }

        public ICollectionView CreatedThreadsView
        {
            get { return _createdThreadsView; }
            set
            {
                _createdThreadsView = value;
                RaisePropertyChanged("CreatedThreadsView");
            }
        }

        public ICollectionView WaitingThreadsView
        {
            get { return _waitingThreadsView; }
            set
            {
                _waitingThreadsView = value;
                RaisePropertyChanged("WaitingThreadsView");
            }
        }

        public ICollectionView WorkingThreadsView
        {
            get { return _workingThreadsView; }
            set
            {
                _workingThreadsView = value;
                RaisePropertyChanged("WorkingThreadsView");
            }
        }

        public decimal SemaphoreCapacity
        {
            get { return _semaphoreCapacity; }
            set
            {
                if (value > _semaphoreCapacity)
                {
                    _semaphore.Release(int.Parse((value - _semaphoreCapacity).ToString()));
                }
                else
                {
                    for (int i = 0; i < int.Parse((_semaphoreCapacity - value).ToString()); i++)
                    {
                        if (Threads.Count(t => t.Status == Status.Working) > value)
                        {
                            RemoveThread(Threads.Where(t => t.Counter == Threads.Max(z => z.Counter)).First());
                        }
                        else
                        {
                            _semaphore.WaitAsync();
                        }
                    }
                }

                _semaphoreCapacity = value;

                RefreshView();

                RaisePropertyChanged("SemaphoreCapacity");
            }
        }

        private void CreateThread()
        {
            SemaphoreThread thread = new SemaphoreThread(_counter++, _semaphore);
            thread.ThreadInfoChanged += RefreshView;
            Threads.Add(thread);
            RefreshView();
        }

        private void MoveThread()
        {
            switch (CurrentThread.Status)
            {
                case Status.Working:
                    _semaphore.Release();
                    RemoveThread(CurrentThread);
                    RefreshView();
                    break;
                case Status.Created:
                    CurrentThread.Status = Status.Waiting;
                    CurrentThread.InnerThread.Start();
                    RefreshView();
                    break;
                default:
                    break;
            }
        }

        private void RefreshView()
        {
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                CreatedThreadsView.Refresh();
                WaitingThreadsView.Refresh();
                WorkingThreadsView.Refresh();
            });
        }

        private void RemoveThread(SemaphoreThread thread)
        {
            thread.Dispose();
            Threads.Remove(thread);
        }
    }
}