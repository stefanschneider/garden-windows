package lifecycle_test

import (
	"bytes"
	"os"
	"time"

	"github.com/cloudfoundry-incubator/garden"
	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"
)

var _ = Describe("Lifecycle", func() {
	var c garden.Container
	var err error

	JustBeforeEach(func() {
		client = startGarden(true)
		c, err = client.Create(garden.ContainerSpec{})
		Expect(err).ToNot(HaveOccurred())
	})

	AfterEach(func() {
		err := client.Destroy(c.Handle())
		Expect(err).ShouldNot(HaveOccurred())
	})

	Describe("process pid", func() {
		var process garden.Process
		var pid uint32

		JustBeforeEach(func() {
			tarFile, err := os.Open("../bin/loop.tar.gz")
			Expect(err).ShouldNot(HaveOccurred())
			defer tarFile.Close()

			err = c.StreamIn("bin", tarFile)
			Expect(err).ShouldNot(HaveOccurred())

			process, err = c.Run(garden.ProcessSpec{
				Path: "bin/loop.exe",
			}, garden.ProcessIO{})
			Expect(err).ShouldNot(HaveOccurred())
			pid = process.ID()
		})

		It("returns the pid", func() {
			// NOTE: we have to cast the pid to uint32, otherwise int(0) !=
			// uint32(0) and the following will be trivially true for any
			// value of the pid
			Expect(pid).ToNot(Equal(uint32(0)))
		})

		It("can be used to attach to the process", func() {
			client = restartGarden()
			buf := make([]byte, 0, 1024*1024)
			stdout := bytes.NewBuffer(buf)
			c, err = client.Lookup(c.Handle())
			Expect(err).NotTo(HaveOccurred())
			_, err := c.Attach(pid, garden.ProcessIO{Stdout: stdout})
			Expect(err).NotTo(HaveOccurred())
			time.Sleep(300 * time.Millisecond)
			Expect(stdout.String()).To(ContainSubstring("I'm alive"))
		})
	})

	Describe("handle collisions", func() {
		It("returns an error", func() {
			_, err := client.Create(garden.ContainerSpec{Handle: c.Handle()})
			Expect(err).To(HaveOccurred())
			Expect(err.Error()).To(Equal("handle already exists: " + c.Handle()))
		})
	})
})
