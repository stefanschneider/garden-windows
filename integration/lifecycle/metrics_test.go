package lifecycle_test

import (
	"bytes"
	"os"

	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"

	"github.com/cloudfoundry-incubator/garden"
	uuid "github.com/nu7hatch/gouuid"
)

var _ = Describe("Metrics", func() {
	var gardenArgs []string
	var createContainer func() garden.Container

	BeforeEach(func() {
		createContainer = func() garden.Container {
			handle, err := uuid.NewV4()
			Expect(err).ShouldNot(HaveOccurred())
			container, err := client.Create(garden.ContainerSpec{Handle: handle.String()})
			Expect(err).ShouldNot(HaveOccurred())
			tarFile, err := os.Open("../bin/consume.tar.gz")
			Expect(err).ShouldNot(HaveOccurred())
			defer tarFile.Close()
			err = container.StreamIn("bin", tarFile)
			Expect(err).ShouldNot(HaveOccurred())
			return container
		}
		gardenArgs = []string{}
		client = startGarden(gardenArgs...)
	})

	Describe("a single container", func() {
		var container garden.Container
		BeforeEach(func() {
			container = createContainer()
		})

		AfterEach(func() {
			err := client.Destroy(container.Handle())
			Expect(err).ShouldNot(HaveOccurred())
		})

		It("returns memory stats", func() {
			buf := make([]byte, 0, 1024*1024)
			stdout := bytes.NewBuffer(buf)

			process, err := container.Run(garden.ProcessSpec{
				Path: "bin/consume.exe",
				Args: []string{"memory", "64"},
			}, garden.ProcessIO{Stdout: stdout})
			Expect(err).ShouldNot(HaveOccurred())

			metrics, err := container.Metrics()
			Expect(err).ToNot(HaveOccurred())
			Expect(metrics.MemoryStat.TotalBytesUsed).To(BeNumerically(">", 0))

			process.Wait()
		})
	})
})
