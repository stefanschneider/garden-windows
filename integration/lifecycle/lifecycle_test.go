package lifecycle

import (
	"bytes"
	"fmt"
	"io/ioutil"
	"net/http"
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
		client = startGarden()
		c, err = client.Create(garden.ContainerSpec{})
		Expect(err).ToNot(HaveOccurred())
	})

	AfterEach(func() {
		err := client.Destroy(c.Handle())
		Expect(err).ShouldNot(HaveOccurred())
	})

	Describe("process", func() {
		It("pid is returned", func() {
			tarFile, err := os.Open("../bin/consume.tgz")
			Expect(err).ShouldNot(HaveOccurred())
			defer tarFile.Close()

			err = c.StreamIn("bin", tarFile)
			Expect(err).ShouldNot(HaveOccurred())

			buf := make([]byte, 0, 1024*1024)
			stdout := bytes.NewBuffer(buf)

			process, err := c.Run(garden.ProcessSpec{
				Path: "bin/consume.exe",
				Args: []string{"64"},
			}, garden.ProcessIO{Stdout: stdout})
			Expect(err).ShouldNot(HaveOccurred())
			// NOTE: we have to cast the pid to uint32, otherwise int(0) !=
			// uint32(0) and the following will be trivially true for any
			// value of the pid
			Expect(process.ID()).ToNot(Equal(uint32(0)))
		})

		It("can be signaled", func(done Done) {
			tarFile, err := os.Open("../bin/loop.tgz")
			Expect(err).ShouldNot(HaveOccurred())
			defer tarFile.Close()

			err = c.StreamIn("bin", tarFile)
			Expect(err).ShouldNot(HaveOccurred())

			process, err := c.Run(garden.ProcessSpec{
				Path: "bin/loop.exe",
			}, garden.ProcessIO{})
			Expect(err).ShouldNot(HaveOccurred())
			go func() {
				// wait for a second and kill the process
				time.Sleep(1 * time.Second)
				err := process.Signal(garden.SignalKill)
				Expect(err).To(Succeed())
			}()
			process.Wait()
			close(done)
		}, 10.0)
	})

	Describe("handle collisions", func() {
		It("returns an error", func() {
			_, err := client.Create(garden.ContainerSpec{Handle: c.Handle()})
			Expect(err).To(HaveOccurred())
			Expect(err.Error()).To(Equal("handle already exists: " + c.Handle()))
		})
	})

	Describe("running a non IIS/HWC web server", func() {
		FIt("works", func() {
			port, _, err := c.NetIn(8080, 8080)

			tarFile, err := os.Open("../bin/webapp.tgz")
			Expect(err).ShouldNot(HaveOccurred())
			defer tarFile.Close()

			err = c.StreamIn("bin", tarFile)
			Expect(err).ShouldNot(HaveOccurred())
			_, err = c.Run(garden.ProcessSpec{
				Path: "bin/webapp.exe",
				Env:  []string{fmt.Sprintf("PORT=%d", port)},
			}, garden.ProcessIO{})
			Expect(err).ShouldNot(HaveOccurred())

			curl := func() string {
				response, err := http.Get(fmt.Sprintf("http://localhost:%d", port))
				if err != nil {
					return ""
				}
				defer response.Body.Close()
				bytes, err := ioutil.ReadAll(response.Body)
				if err != nil {
					return ""
				}
				return string(bytes)
			}

			Eventually(curl).Should(Equal("foo"))
		})
	})
})
