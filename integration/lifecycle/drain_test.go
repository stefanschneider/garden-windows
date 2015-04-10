package lifecycle_test

import (
	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"
	"github.com/onsi/gomega/gbytes"

	"github.com/cloudfoundry-incubator/garden"
)

var _ = Describe("Through a restart", func() {
	var container garden.Container
	var gardenArgs []string
	var privileged bool

	BeforeEach(func() {
		gardenArgs = []string{}
		privileged = false
	})

	JustBeforeEach(func() {
		// TODO: start containerizer
		client = startGarden(gardenArgs...)

		var err error

		container, err = client.Create(garden.ContainerSpec{Privileged: privileged})
		Expect(err).ShouldNot(HaveOccurred())
	})

	AfterEach(func() {
		if container != nil {
			err := client.Destroy(container.Handle())
			Expect(err).ShouldNot(HaveOccurred())
		}
	})

	Describe("a started process", func() {
		Describe("a memory limit", func() {
			It("is still enforced", func() {
				err := container.LimitMemory(garden.MemoryLimits{4 * 1024 * 1024})
				Expect(err).ShouldNot(HaveOccurred())

				restartGarden(gardenArgs...)

				process, err := container.Run(garden.ProcessSpec{
					Path: "sh",
					Args: []string{"-c", "echo $(yes | head -c 67108864); echo goodbye; exit 42"},
				}, garden.ProcessIO{})
				Expect(err).ShouldNot(HaveOccurred())

				// cgroups OOM killer seems to leave no trace of the process;
				// there's no exit status indicator, so just assert that the one
				// we tried to exit with after over-allocating is not seen

				Expect(process.Wait()).ShouldNot(Equal(42), "process did not get OOM killed")
			})
		})

		Describe("a container's list of events", func() {
			It("is still reported", func() {
				err := container.LimitMemory(garden.MemoryLimits{4 * 1024 * 1024})
				Expect(err).ShouldNot(HaveOccurred())

				// trigger 'out of memory' event
				process, err := container.Run(garden.ProcessSpec{
					Path: "sh",
					Args: []string{"-c", "echo $(yes | head -c 67108864); echo goodbye; exit 42"},
				}, garden.ProcessIO{})
				Expect(err).ShouldNot(HaveOccurred())

				Expect(process.Wait()).ShouldNot(Equal(42), "process did not get OOM killed")

				Eventually(func() []string {
					info, err := container.Info()
					Expect(err).ShouldNot(HaveOccurred())

					return info.Events
				}).Should(ContainElement("out of memory"))

				restartGarden(gardenArgs...)

				info, err := container.Info()
				Expect(err).ShouldNot(HaveOccurred())

				Expect(info.Events).Should(ContainElement("out of memory"))
			})
		})

		Describe("a container's user", func() {
			It("does not get reused", func() {
				idA := gbytes.NewBuffer()
				idB := gbytes.NewBuffer()

				processA, err := container.Run(garden.ProcessSpec{
					Path: "id",
					Args: []string{"-u"},
				}, garden.ProcessIO{
					Stdout: idA,
				})
				Expect(err).ShouldNot(HaveOccurred())

				Expect(processA.Wait()).Should(Equal(0))

				restartGarden(gardenArgs...)

				otherContainer, err := client.Create(garden.ContainerSpec{})
				Expect(err).ShouldNot(HaveOccurred())

				processB, err := otherContainer.Run(garden.ProcessSpec{
					Path: "id",
					Args: []string{"-u"},
				}, garden.ProcessIO{Stdout: idB})
				Expect(err).ShouldNot(HaveOccurred())

				Expect(processB.Wait()).Should(Equal(0))

				Expect(idA.Contents()).ShouldNot(Equal(idB.Contents()))
			})
		})
	})
})
