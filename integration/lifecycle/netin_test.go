package lifecycle_test

import (
	"bytes"
	"os"

	"github.com/cloudfoundry-incubator/garden"
	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"
)

var _ = Describe("NetIn", func() {
	var c garden.Container
	var err error

	JustBeforeEach(func() {
		client = startGarden()
		c, err = client.Create(garden.ContainerSpec{})
		Expect(err).ToNot(HaveOccurred())
	})

	AfterEach(func() {
		err := client.Destroy(c.Handle())
		Expect(err).ShouldNot(HaveOccurred())
	})

	It("returns the port", func() {
		a, b, err := c.NetIn(8765, 9876)
		Expect(err).ShouldNot(HaveOccurred())

		var expectedHost, expectedContainerPort uint32
		expectedHost, expectedContainerPort = 8765, 9876
		Expect(a).To(Equal(expectedHost))
		Expect(b).To(Equal(expectedContainerPort))

		a, b, err = c.NetIn(18765, 19876)
		Expect(err).ShouldNot(HaveOccurred())

		expectedHost, expectedContainerPort = 18765, 19876
		Expect(a).To(Equal(expectedHost))
		Expect(b).To(Equal(expectedContainerPort))

		tarFile, err := os.Open("../bin/show_port.tgz")
		Expect(err).ShouldNot(HaveOccurred())
		defer tarFile.Close()

		err = c.StreamIn("bin", tarFile)
		Expect(err).ShouldNot(HaveOccurred())

		buf := make([]byte, 0, 1024*1024)
		bufTwo := make([]byte, 0, 1024*1024)
		stdout := bytes.NewBuffer(buf)
		stdoutTwo := bytes.NewBuffer(bufTwo)

		process, err := c.Run(garden.ProcessSpec{
			Path: "bin/show_port.bat",
			Env:  []string{"PORT=9876"},
		}, garden.ProcessIO{Stdout: stdout})
		Expect(err).ShouldNot(HaveOccurred())
		_, err = process.Wait()
		Expect(err).ShouldNot(HaveOccurred())
		Expect(stdout).Should(ContainSubstring("PORT=8765"))

		process, err = c.Run(garden.ProcessSpec{
			Path: "bin/show_port.bat",
			Env:  []string{"PORT=19876"},
		}, garden.ProcessIO{Stdout: stdoutTwo})
		Expect(err).ShouldNot(HaveOccurred())
		_, err = process.Wait()
		Expect(err).ShouldNot(HaveOccurred())
		Expect(stdoutTwo).Should(ContainSubstring("PORT=18765"))
	})
})
