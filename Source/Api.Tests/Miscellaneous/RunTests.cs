﻿using System;
using System.Threading.Tasks;
using CodeSmith.Core.Helpers;
using Xunit;

namespace Exceptionless.Api.Tests.Miscelaneous {
    public class RunTests {
        [Fact]
        public void CanRunOnceWithConcurrency() {
            int counter = 0;
            Action method1 = () => counter++;
            Action method2 = () => counter += 2;
            Parallel.For(0, 100, i => Run.Once(method1));
            Assert.Equal(1, counter);
            Run.Once(method1);
            Assert.Equal(1, counter);
            Run.Once(method2);
            Assert.Equal(3, counter);
        }

        [Fact]
        public void CanRunOnceWithMethod() {
            _counter = 0;
            Run.Once(TestMethod1);
            Assert.Equal(1, _counter);
            Run.Once(TestMethod1);
            Assert.Equal(1, _counter);
            Run.Once(TestMethod2);
            Assert.Equal(3, _counter);
            Task.Run(() => Run.Once(TestMethod1)).Wait();
            Assert.Equal(3, _counter);
        }

        [Fact]
        public void CanRunOnceWithLocalAction() {
            int counter = 0;
            Action method1 = () => counter++;
            Action method2 = () => counter += 2;
            Run.Once(method1);
            Assert.Equal(1, counter);
            Run.Once(method1);
            Assert.Equal(1, counter);
            Run.Once(method2);
            Assert.Equal(3, counter);
        }

        [Fact]
        public void CanRunWithRetries() {
            int attempts = 0;
            Assert.Throws(typeof(ApplicationException), () => Run.WithRetries(() => {
                attempts++;
                throw new ApplicationException();
            }));
            Assert.Equal(3, attempts);
        }

        private int _counter = 0;
        private void TestMethod1() {
            _counter++;
        }

        private void TestMethod2() {
            _counter += 2;
        }
    }
}
