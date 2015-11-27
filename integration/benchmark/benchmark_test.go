package benchmark_test

import (
	"github.com/cloudfoundry-incubator/garden"
	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"
	"os"
)

var _ = Describe("Benchmark", func() {
	var c garden.Container
	var err error

	BeforeEach(func() {
		client = startGarden()
		c, err = client.Create(garden.ContainerSpec{})
		Expect(err).ToNot(HaveOccurred())
	})

	AfterEach(func() {
		err := client.Destroy(c.Handle())
		Expect(err).ShouldNot(HaveOccurred())
	})

	Describe("StreamIn", func() {
		Measure("it should stream in fast", func(b Benchmarker) {
			runtime := b.Time("runtime", func() {
				tarFile, err := os.Open("../bin/consume.tgz")
				Expect(err).ShouldNot(HaveOccurred())
				defer tarFile.Close()

				err = c.StreamIn(garden.StreamInSpec{Path: "bin", TarStream: tarFile})
				Expect(err).ShouldNot(HaveOccurred())
			})

			Expect(runtime.Seconds()).Should(BeNumerically("<", 100), "StreamIn() shouldn't take too long.")
		}, 10)

		XMeasure("it should stream in big files fast", func(b Benchmarker) {
			runtime := b.Time("runtime", func() {
				tarFile, err := os.Open("../bin/garden-linux-release-0.325.0.tgz")
				Expect(err).ShouldNot(HaveOccurred())
				defer tarFile.Close()

				err = c.StreamIn(garden.StreamInSpec{Path: "bin", TarStream: tarFile})
				Expect(err).ShouldNot(HaveOccurred())
			})

			Expect(runtime.Seconds()).Should(BeNumerically("<", 300), "StreamIn() shouldn't take too long.")
		}, 10)

	})

	Describe("Container Metrics", func() {
		BeforeEach(func() {
			for _, f := range []string{"../bin/loop.tgz", "../bin/launcher.tgz"} {
				tarFile, err := os.Open(f)
				Expect(err).ShouldNot(HaveOccurred())
				defer tarFile.Close()

				err = c.StreamIn(garden.StreamInSpec{Path: "bin", TarStream: tarFile})
				Expect(err).ShouldNot(HaveOccurred())
			}

			_, err := c.Run(garden.ProcessSpec{
				Path: "bin/launcher.exe",
				Args: []string{"bin/loop.exe"},
			}, garden.ProcessIO{})
			Expect(err).ShouldNot(HaveOccurred())
		})

		Measure("it should get container metrics fast", func(b Benchmarker) {
			runtime := b.Time("runtime", func() {
				for i := 1; i < 100; i++ {
					_, err := c.Metrics()
					Expect(err).ToNot(HaveOccurred())
				}
			})

			Expect(runtime.Seconds()).Should(BeNumerically("<", 10), "StreamIn() shouldn't take too long.")
		}, 3)

	})
})
