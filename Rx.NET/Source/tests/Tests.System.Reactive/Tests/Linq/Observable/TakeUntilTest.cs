﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Xunit;
using ReactiveTests.Dummies;
using System.Reflection;
using System.Threading;
using System.Reactive.Disposables;
using System.Reactive.Subjects;

namespace ReactiveTests.Tests
{
    public class TakeUntilTest : ReactiveTest
    {
        #region + Observable +

        [Fact]
        public void TakeUntil_ArgumentChecking()
        {
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.TakeUntil<int, int>(null, DummyObservable<int>.Instance));
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.TakeUntil<int, int>(DummyObservable<int>.Instance, null));
        }

        [Fact]
        public void TakeUntil_Preempt_SomeData_Next()
        {
            var scheduler = new TestScheduler();

            var l = scheduler.CreateHotObservable(
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnNext(240, 5),
                OnCompleted<int>(250)
            );

            var r = scheduler.CreateHotObservable(
                OnNext(150, 1),
                OnNext(225, 99),
                OnCompleted<int>(230)
            );

            var res = scheduler.Start(() =>
                l.TakeUntil(r)
            );

            res.Messages.AssertEqual(
                OnNext(210, 2),
                OnNext(220, 3),
                OnCompleted<int>(225)
            );

            l.Subscriptions.AssertEqual(
                Subscribe(200, 225)
            );

            r.Subscriptions.AssertEqual(
                Subscribe(200, 225)
            );
        }

        [Fact]
        public void TakeUntil_Preempt_SomeData_Error()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var l = scheduler.CreateHotObservable(
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnNext(240, 5),
                OnCompleted<int>(250)
            );

            var r = scheduler.CreateHotObservable(
                OnNext(150, 1),
                OnError<int>(225, ex)
            );

            var res = scheduler.Start(() =>
                l.TakeUntil(r)
            );

            res.Messages.AssertEqual(
                OnNext(210, 2),
                OnNext(220, 3),
                OnError<int>(225, ex)
            );

            l.Subscriptions.AssertEqual(
                Subscribe(200, 225)
            );

            r.Subscriptions.AssertEqual(
                Subscribe(200, 225)
            );
        }

        [Fact]
        public void TakeUntil_NoPreempt_SomeData_Empty()
        {
            var scheduler = new TestScheduler();

            var l = scheduler.CreateHotObservable(
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnNext(240, 5),
                OnCompleted<int>(250)
            );

            var r = scheduler.CreateHotObservable(
                OnNext(150, 1),
                OnCompleted<int>(225)
            );

            var res = scheduler.Start(() =>
                l.TakeUntil(r)
            );

            res.Messages.AssertEqual(
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnNext(240, 5),
                OnCompleted<int>(250)
            );

            l.Subscriptions.AssertEqual(
                Subscribe(200, 250)
            );

            r.Subscriptions.AssertEqual(
                Subscribe(200, 225)
            );
        }

        [Fact]
        public void TakeUntil_NoPreempt_SomeData_Never()
        {
            var scheduler = new TestScheduler();

            var l = scheduler.CreateHotObservable(
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnNext(240, 5),
                OnCompleted<int>(250)
            );

            var r = scheduler.CreateHotObservable(
                OnNext(150, 1)
            );

            var res = scheduler.Start(() =>
                l.TakeUntil(r)
            );

            res.Messages.AssertEqual(
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnNext(240, 5),
                OnCompleted<int>(250)
            );

            l.Subscriptions.AssertEqual(
                Subscribe(200, 250)
            );

            r.Subscriptions.AssertEqual(
                Subscribe(200, 250)
            );
        }

        [Fact]
        public void TakeUntil_Preempt_Never_Next()
        {
            var scheduler = new TestScheduler();

            var l = scheduler.CreateHotObservable(
                OnNext(150, 1)
            );

            var r = scheduler.CreateHotObservable(
                OnNext(150, 1),
                OnNext(225, 2), //!
                OnCompleted<int>(250)
            );

            var res = scheduler.Start(() =>
                l.TakeUntil(r)
            );

            res.Messages.AssertEqual(
                OnCompleted<int>(225)
            );

            l.Subscriptions.AssertEqual(
                Subscribe(200, 225)
            );

            r.Subscriptions.AssertEqual(
                Subscribe(200, 225)
            );
        }

        [Fact]
        public void TakeUntil_Preempt_Never_Error()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var l = scheduler.CreateHotObservable(
                OnNext(150, 1)
            );

            var r = scheduler.CreateHotObservable(
                OnNext(150, 1),
                OnError<int>(225, ex)
            );

            var res = scheduler.Start(() =>
                l.TakeUntil(r)
            );

            res.Messages.AssertEqual(
                OnError<int>(225, ex)
            );

            l.Subscriptions.AssertEqual(
                Subscribe(200, 225)
            );

            r.Subscriptions.AssertEqual(
                Subscribe(200, 225)
            );
        }

        [Fact]
        public void TakeUntil_NoPreempt_Never_Empty()
        {
            var scheduler = new TestScheduler();

            var l = scheduler.CreateHotObservable(
                OnNext(150, 1)
            );

            var r = scheduler.CreateHotObservable(
                OnNext(150, 1),
                OnCompleted<int>(225)
            );

            var res = scheduler.Start(() =>
                l.TakeUntil(r)
            );

            res.Messages.AssertEqual(
            );

            l.Subscriptions.AssertEqual(
                Subscribe(200, 1000 /* can't dispose prematurely, could be in flight to dispatch OnError */)
            );

            r.Subscriptions.AssertEqual(
                Subscribe(200, 225)
            );
        }

        [Fact]
        public void TakeUntil_NoPreempt_Never_Never()
        {
            var scheduler = new TestScheduler();

            var l = scheduler.CreateHotObservable(
                OnNext(150, 1)
            );

            var r = scheduler.CreateHotObservable(
                OnNext(150, 1)
            );

            var res = scheduler.Start(() =>
                l.TakeUntil(r)
            );

            res.Messages.AssertEqual(
            );

            l.Subscriptions.AssertEqual(
                Subscribe(200, 1000)
            );

            r.Subscriptions.AssertEqual(
                Subscribe(200, 1000)
            );
        }

        [Fact]
        public void TakeUntil_Preempt_BeforeFirstProduced()
        {
            var scheduler = new TestScheduler();

            var l = scheduler.CreateHotObservable(
                OnNext(150, 1),
                OnNext(230, 2),
                OnCompleted<int>(240)
            );

            var r = scheduler.CreateHotObservable(
                OnNext(150, 1),
                OnNext(210, 2), //!
                OnCompleted<int>(220)
            );

            var res = scheduler.Start(() =>
                l.TakeUntil(r)
            );

            res.Messages.AssertEqual(
                OnCompleted<int>(210)
            );

            l.Subscriptions.AssertEqual(
                Subscribe(200, 210)
            );

            r.Subscriptions.AssertEqual(
                Subscribe(200, 210)
            );
        }

        [Fact]
        public void TakeUntil_Preempt_BeforeFirstProduced_RemainSilentAndProperDisposed()
        {
            var scheduler = new TestScheduler();

            bool sourceNotDisposed = false;

            var l = scheduler.CreateHotObservable(
                OnNext(150, 1),
                OnError<int>(215, new Exception()), // should not come
                OnCompleted<int>(240)
            );

            var r = scheduler.CreateHotObservable(
                OnNext(150, 1),
                OnNext(210, 2), //!
                OnCompleted<int>(220)
            );

            var res = scheduler.Start(() =>
                l.Do(_ => sourceNotDisposed = true).TakeUntil(r)
            );

            res.Messages.AssertEqual(
                OnCompleted<int>(210)
            );

            Assert.False(sourceNotDisposed);
        }

        [Fact]
        public void TakeUntil_NoPreempt_AfterLastProduced_ProperDisposedSignal()
        {
            var scheduler = new TestScheduler();

            bool signalNotDisposed = false;

            var l = scheduler.CreateHotObservable(
                OnNext(150, 1),
                OnNext(230, 2),
                OnCompleted<int>(240)
            );

            var r = scheduler.CreateHotObservable(
                OnNext(150, 1),
                OnNext(250, 2),
                OnCompleted<int>(260)
            );

            var res = scheduler.Start(() =>
                l.TakeUntil(r.Do(_ => signalNotDisposed = true))
            );

            res.Messages.AssertEqual(
                OnNext(230, 2),
                OnCompleted<int>(240)
            );

            Assert.False(signalNotDisposed);
        }

        [Fact]
        public void TakeUntil_Error_Some()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var l = scheduler.CreateHotObservable(
                OnNext(150, 1),
                OnError<int>(225, ex)
            );

            var r = scheduler.CreateHotObservable(
                OnNext(150, 1),
                OnNext<int>(240, 2)
            );

            var res = scheduler.Start(() =>
                l.TakeUntil(r)
            );

            res.Messages.AssertEqual(
                OnError<int>(225, ex)
            );

            l.Subscriptions.AssertEqual(
                Subscribe(200, 225)
            );

            r.Subscriptions.AssertEqual(
                Subscribe(200, 225)
            );
        }

        [Fact]
        public void TakeUntil_Immediate()
        {
            var scheduler = new TestScheduler();

            var xs = Observable.Return(1);
            var ys = Observable.Return("bar");

            var res = scheduler.Start(() =>
                xs.TakeUntil(ys)
            );

            res.Messages.AssertEqual(
                OnCompleted<int>(200)
            );
        }
        #endregion

        #region + Timed +

        [Fact]
        public void TakeUntil_Timed_ArgumentChecking()
        {
            var xs = Observable.Return(42);

            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.TakeUntil(default(IObservable<int>), DateTimeOffset.Now));

            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.TakeUntil(default(IObservable<int>), DateTimeOffset.Now, Scheduler.Default));
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.TakeUntil(xs, DateTimeOffset.Now, default(IScheduler)));
        }

        [Fact]
        public void TakeUntil_Zero()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable<int>(
                OnNext(210, 1),
                OnNext(220, 2),
                OnCompleted<int>(230)
            );

            var res = scheduler.Start(() =>
                xs.TakeUntil(new DateTimeOffset(), scheduler)
            );

            res.Messages.AssertEqual(
                OnCompleted<int>(201)
            );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 201)
            );
        }

        [Fact]
        public void TakeUntil_Some()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable<int>(
                OnNext(210, 1),
                OnNext(220, 2),
                OnNext(230, 3),
                OnCompleted<int>(240)
            );

            var res = scheduler.Start(() =>
                xs.TakeUntil(new DateTimeOffset(225, TimeSpan.Zero), scheduler)
            );

            res.Messages.AssertEqual(
                OnNext(210, 1),
                OnNext(220, 2),
                OnCompleted<int>(225)
            );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 225)
            );
        }

        [Fact]
        public void TakeUntil_Late()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable<int>(
                OnNext(210, 1),
                OnNext(220, 2),
                OnCompleted<int>(230)
            );

            var res = scheduler.Start(() =>
                xs.TakeUntil(new DateTimeOffset(250, TimeSpan.Zero), scheduler)
            );

            res.Messages.AssertEqual(
                OnNext(210, 1),
                OnNext(220, 2),
                OnCompleted<int>(230)
            );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 230)
            );
        }

        [Fact]
        public void TakeUntil_Error()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var xs = scheduler.CreateHotObservable<int>(
                OnError<int>(210, ex)
            );

            var res = scheduler.Start(() =>
                xs.TakeUntil(new DateTimeOffset(250, TimeSpan.Zero), scheduler)
            );

            res.Messages.AssertEqual(
                OnError<int>(210, ex)
            );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 210)
            );
        }

        [Fact]
        public void TakeUntil_Never()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var xs = scheduler.CreateHotObservable<int>(
            );

            var res = scheduler.Start(() =>
                xs.TakeUntil(new DateTimeOffset(250, TimeSpan.Zero), scheduler)
            );

            res.Messages.AssertEqual(
                OnCompleted<int>(250)
            );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 250)
            );
        }

        [Fact]
        public void TakeUntil_Twice1()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var xs = scheduler.CreateHotObservable<int>(
                OnNext(210, 1),
                OnNext(220, 2),
                OnNext(230, 3),
                OnNext(240, 4),
                OnNext(250, 5),
                OnNext(260, 6),
                OnCompleted<int>(270)
            );

            var res = scheduler.Start(() =>
                xs.TakeUntil(new DateTimeOffset(255, TimeSpan.Zero), scheduler).TakeUntil(new DateTimeOffset(235, TimeSpan.Zero), scheduler)
            );

            res.Messages.AssertEqual(
                OnNext(210, 1),
                OnNext(220, 2),
                OnNext(230, 3),
                OnCompleted<int>(235)
            );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 235)
            );
        }

        [Fact]
        public void TakeUntil_Twice2()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var xs = scheduler.CreateHotObservable<int>(
                OnNext(210, 1),
                OnNext(220, 2),
                OnNext(230, 3),
                OnNext(240, 4),
                OnNext(250, 5),
                OnNext(260, 6),
                OnCompleted<int>(270)
            );

            var res = scheduler.Start(() =>
                xs.TakeUntil(new DateTimeOffset(235, TimeSpan.Zero), scheduler).TakeUntil(new DateTimeOffset(255, TimeSpan.Zero), scheduler)
            );

            res.Messages.AssertEqual(
                OnNext(210, 1),
                OnNext(220, 2),
                OnNext(230, 3),
                OnCompleted<int>(235)
            );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 235)
            );
        }

        [Fact]
        public void TakeUntil_Default()
        {
            var xs = Observable.Range(0, 10, Scheduler.Default);

            var res = xs.TakeUntil(DateTimeOffset.Now.AddMinutes(1));

            var e = new ManualResetEvent(false);

            var lst = new List<int>();
            res.Subscribe(
                lst.Add,
                () => e.Set()
            );

            e.WaitOne();

            Assert.True(lst.SequenceEqual(Enumerable.Range(0, 10)));
        }

        #endregion

    }
}