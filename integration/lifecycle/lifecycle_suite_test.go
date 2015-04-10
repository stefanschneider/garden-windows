package lifecycle_test

import (
	"fmt"
	"os"
	"os/exec"
	"syscall"
	"testing"

	"github.com/cloudfoundry-incubator/garden"
	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"
	"github.com/onsi/gomega/gexec"
	"github.com/tedsuo/ifrit"

	"github.com/cloudfoundry-incubator/garden-windows/integration/runner"
)

var gardenBin string

var gardenRunner *runner.Runner
var gardenProcess ifrit.Process

var client garden.Client

func startGarden(argv ...string) garden.Client {
	gardenAddr := fmt.Sprintf("127.0.0.1:4567")

	tmpDir := os.TempDir()
	gardenRunner = runner.New("tcp4", gardenAddr, tmpDir, gardenBin, "http://127.0.0.1:8080")

	gardenProcess = ifrit.Invoke(gardenRunner)

	return gardenRunner.NewClient()
}

func restartGarden(argv ...string) {
	Expect(client.Ping()).Should(Succeed(), "tried to restart garden while it was not running")
	gardenProcess.Signal(syscall.SIGTERM)
	Eventually(gardenProcess.Wait(), 10).Should(Receive())

	startGarden(argv...)
}

func ensureGardenRunning() {
	if err := client.Ping(); err != nil {
		client = startGarden()
	}
	Expect(client.Ping()).ShouldNot(HaveOccurred())
}

func TestLifecycle(t *testing.T) {
	SynchronizedBeforeSuite(func() []byte {
		gardenPath, err := gexec.Build("github.com/cloudfoundry-incubator/garden-windows", "-a", "-race", "-tags", "daemon")
		Expect(err).ShouldNot(HaveOccurred())
		return []byte(gardenPath)
	}, func(gardenPath []byte) {
		gardenBin = string(gardenPath)
	})

	AfterEach(func() {
		ensureGardenRunning()
		gardenProcess.Signal(syscall.SIGQUIT)
		Eventually(gardenProcess.Wait(), 10).Should(Receive())
	})

	SynchronizedAfterSuite(func() {
		//noop
	}, func() {
		gexec.CleanupBuildArtifacts()
	})

	RegisterFailHandler(Fail)
	RunSpecs(t, "Lifecycle Suite")
}

func containerIP(ctr garden.Container) string {
	info, err := ctr.Info()
	Expect(err).ShouldNot(HaveOccurred())
	return info.ContainerIP
}

func dumpIP() {
	cmd := exec.Command("ip", "a")
	op, err := cmd.CombinedOutput()
	Expect(err).ShouldNot(HaveOccurred())
	fmt.Println("IP status:\n", string(op))

	cmd = exec.Command("iptables", "--list")
	op, err = cmd.CombinedOutput()
	Expect(err).ShouldNot(HaveOccurred())
	fmt.Println("IP tables chains:\n", string(op))

	cmd = exec.Command("iptables", "--list-rules")
	op, err = cmd.CombinedOutput()
	Expect(err).ShouldNot(HaveOccurred())
	fmt.Println("IP tables rules:\n", string(op))
}
